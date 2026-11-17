using HotelReservas.Models;
using Microsoft.Data.SqlClient;

namespace HotelReservas.Data;

public class ReservaRepository
{
    private readonly string _cs;

    public ReservaRepository(IConfiguration config)
    {
        _cs = config.GetConnectionString("Hotel")
              ?? throw new InvalidOperationException("Falta la cadena de conexión 'Hotel'.");
    }

    // Caso de uso incluido: Buscar Cliente
    public Cliente? BuscarCliente(string codigo)
    {
        using var cn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            "SELECT CodCliente, Nombres, Apellidos FROM Cliente WHERE CodCliente = @cod", cn);
        cmd.Parameters.AddWithValue("@cod", codigo);
        cn.Open();
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;
        return new Cliente
        {
            CodCliente = rd.GetString(0),
            Nombres = rd.GetString(1),
            Apellidos = rd.GetString(2)
        };
    }

    // Punto de extensión: Mantener Clientes - Agregar Cliente
    public void AgregarCliente(Cliente c)
    {
        using var cn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            "INSERT INTO Cliente (CodCliente, Nombres, Apellidos) VALUES (@cod, @nom, @ape)", cn);
        cmd.Parameters.AddWithValue("@cod", c.CodCliente);
        cmd.Parameters.AddWithValue("@nom", c.Nombres);
        cmd.Parameters.AddWithValue("@ape", c.Apellidos ?? "");
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    // Caso de uso incluido: Buscar Habitación (solo las disponibles)
    public List<Habitacion> HabitacionesDisponibles()
    {
        var lista = new List<Habitacion>();
        using var cn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            "SELECT NumHabitacion, Tipo, CostoDia, Estado FROM Habitacion " +
            "WHERE Estado = 'Disponible' ORDER BY NumHabitacion", cn);
        cn.Open();
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            lista.Add(new Habitacion
            {
                NumHabitacion = rd.GetString(0),
                Tipo = rd.GetString(1),
                CostoDia = rd.GetDecimal(2),
                Estado = rd.GetString(3)
            });
        }
        return lista;
    }

    // Trae el costo real desde la BD (no confiamos en el que manda el navegador)
    public Habitacion? ObtenerHabitacionDisponible(string numero)
    {
        using var cn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            "SELECT NumHabitacion, Tipo, CostoDia, Estado FROM Habitacion " +
            "WHERE NumHabitacion = @n AND Estado = 'Disponible'", cn);
        cmd.Parameters.AddWithValue("@n", numero);
        cn.Open();
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;
        return new Habitacion
        {
            NumHabitacion = rd.GetString(0),
            Tipo = rd.GetString(1),
            CostoDia = rd.GetDecimal(2),
            Estado = rd.GetString(3)
        };
    }

    // Pasos 16 y 17 del flujo: genera el número, graba reserva + detalle
    // y deja las habitaciones en estado "Reservado" (todo en una transacción).
    public int GrabarReserva(Reserva r)
    {
        using var cn = new SqlConnection(_cs);
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            // último número de reserva + 1
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(MAX(NroReserva), 0) + 1 FROM Reserva WITH (UPDLOCK, HOLDLOCK)", cn, tx))
            {
                r.NroReserva = Convert.ToInt32(cmd.ExecuteScalar());
            }

            using (var cmd = new SqlCommand(
                "INSERT INTO Reserva (NroReserva, CodCliente, FechaLlegada, FechaSalida, CantidadDias, SubTotal, IGV, MontoTotal) " +
                "VALUES (@nro, @cli, @lleg, @sal, @dias, @sub, @igv, @tot)", cn, tx))
            {
                cmd.Parameters.AddWithValue("@nro", r.NroReserva);
                cmd.Parameters.AddWithValue("@cli", r.CodCliente);
                cmd.Parameters.AddWithValue("@lleg", r.FechaLlegada.Date);
                cmd.Parameters.AddWithValue("@sal", r.FechaSalida.Date);
                cmd.Parameters.AddWithValue("@dias", r.CantidadDias);
                cmd.Parameters.AddWithValue("@sub", r.SubTotal);
                cmd.Parameters.AddWithValue("@igv", r.IGV);
                cmd.Parameters.AddWithValue("@tot", r.MontoTotal);
                cmd.ExecuteNonQuery();
            }

            foreach (var d in r.Detalle)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO DetalleReserva (NroReserva, NumHabitacion, NombreHuesped, CostoDia, Dias, MontoPagar) " +
                    "VALUES (@nro, @hab, @hue, @costo, @dias, @monto)", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@nro", r.NroReserva);
                    cmd.Parameters.AddWithValue("@hab", d.NumHabitacion);
                    cmd.Parameters.AddWithValue("@hue", d.NombreHuesped);
                    cmd.Parameters.AddWithValue("@costo", d.CostoDia);
                    cmd.Parameters.AddWithValue("@dias", d.Dias);
                    cmd.Parameters.AddWithValue("@monto", d.MontoPagar);
                    cmd.ExecuteNonQuery();
                }

                // actualizarEstado("Reservado") solo si seguía disponible
                using (var cmd = new SqlCommand(
                    "UPDATE Habitacion SET Estado = 'Reservado' " +
                    "WHERE NumHabitacion = @hab AND Estado = 'Disponible'", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@hab", d.NumHabitacion);
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException(
                            $"La habitación {d.NumHabitacion} ya no está disponible.");
                }
            }

            tx.Commit();
            return r.NroReserva;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
