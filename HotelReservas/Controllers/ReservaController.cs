using HotelReservas.Data;
using HotelReservas.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservas.Controllers;

// Clase de control del análisis: ctrlReserva
public class ReservaController : Controller
{
    private readonly ReservaRepository _repo;

    public ReservaController(ReservaRepository repo) => _repo = repo;

    // Paso 1: muestra la interfaz RESERVA
    public IActionResult Index() => View();

    // Pasos 2-4: buscarCliente(codCliente)
    [HttpGet]
    public IActionResult BuscarCliente(string codigo)
    {
        var c = _repo.BuscarCliente(codigo?.Trim() ?? "");
        if (c is null) return NotFound(new { mensaje = "Cliente no existe" });
        return Json(new { c.CodCliente, c.Nombres, c.Apellidos, nombreCompleto = c.NombreCompleto });
    }

    // Flujo alterno: Cliente no existe -> registrar nuevo cliente
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult AgregarCliente([FromBody] Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.CodCliente) || string.IsNullOrWhiteSpace(c.Nombres))
            return BadRequest(new { mensaje = "Código y nombres son obligatorios." });
        if (_repo.BuscarCliente(c.CodCliente) is not null)
            return Conflict(new { mensaje = "Ya existe un cliente con ese código." });

        _repo.AgregarCliente(c);
        return Json(new { c.CodCliente, c.Nombres, c.Apellidos, nombreCompleto = c.NombreCompleto });
    }

    // Pasos 5-7: calcularDias(llegada, salida)
    [HttpGet]
    public IActionResult CalcularDias(DateTime llegada, DateTime salida)
    {
        var dias = Reserva.CalcularDias(llegada, salida);
        if (dias <= 0) return BadRequest(new { mensaje = "La fecha de salida debe ser posterior a la de llegada." });
        return Json(new { dias });
    }

    // Pasos 8-10: buscarHabitacionesDisponibles()
    [HttpGet]
    public IActionResult HabitacionesDisponibles()
    {
        var lista = _repo.HabitacionesDisponibles();
        if (lista.Count == 0) return NotFound(new { mensaje = "No hay habitaciones disponibles" });
        return Json(lista);
    }

    // Pasos 15-18: grabarReserva()
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Grabar([FromBody] GrabarReservaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CodCliente))
            return BadRequest(new { mensaje = "Debe seleccionar un cliente." });
        if (req.Detalle.Count == 0)
            return BadRequest(new { mensaje = "Debe agregar al menos una habitación." });

        var dias = Reserva.CalcularDias(req.FechaLlegada, req.FechaSalida);
        if (dias <= 0)
            return BadRequest(new { mensaje = "Fechas inválidas." });
        if (_repo.BuscarCliente(req.CodCliente) is null)
            return BadRequest(new { mensaje = "Cliente no existe" });

        var reserva = new Reserva
        {
            CodCliente = req.CodCliente,
            FechaLlegada = req.FechaLlegada,
            FechaSalida = req.FechaSalida,
            CantidadDias = dias
        };

        foreach (var item in req.Detalle)
        {
            var hab = _repo.ObtenerHabitacionDisponible(item.NumHabitacion);
            if (hab is null)
                return Conflict(new { mensaje = $"La habitación {item.NumHabitacion} no está disponible." });

            var det = new DetalleReserva
            {
                NumHabitacion = hab.NumHabitacion,
                Tipo = hab.Tipo,
                NombreHuesped = item.NombreHuesped,
                CostoDia = hab.CostoDia,
                Dias = dias
            };
            det.CalcularPago();
            reserva.Detalle.Add(det);
        }

        reserva.CalcularTotales();

        try
        {
            var nro = _repo.GrabarReserva(reserva);
            return Json(new { nroReserva = nro, mensaje = $"Reserva generada con el Nro. {nro}" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    public IActionResult Error() => Content("Ocurrió un error inesperado.");
}
