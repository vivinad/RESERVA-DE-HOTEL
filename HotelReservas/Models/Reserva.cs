namespace HotelReservas.Models;

public class Reserva
{
    public const decimal TasaIGV = 0.18m;

    public int NroReserva { get; set; }
    public string CodCliente { get; set; } = "";
    public DateTime FechaLlegada { get; set; }
    public DateTime FechaSalida { get; set; }
    public int CantidadDias { get; set; }
    public decimal SubTotal { get; set; }
    public decimal IGV { get; set; }
    public decimal MontoTotal { get; set; }
    public List<DetalleReserva> Detalle { get; set; } = new();

    // Mensaje del diagrama: calcularDias(llegada, salida)
    public static int CalcularDias(DateTime llegada, DateTime salida)
        => (salida.Date - llegada.Date).Days;

    // Mensaje del diagrama: calcularTotales(detalle)
    public void CalcularTotales()
    {
        SubTotal = Detalle.Sum(d => d.MontoPagar);
        IGV = Math.Round(SubTotal * TasaIGV, 2);
        MontoTotal = SubTotal + IGV;
    }
}
