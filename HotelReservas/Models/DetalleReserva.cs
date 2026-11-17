namespace HotelReservas.Models;

public class DetalleReserva
{
    public string NumHabitacion { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string NombreHuesped { get; set; } = "";
    public decimal CostoDia { get; set; }
    public int Dias { get; set; }
    public decimal MontoPagar { get; private set; }

    // Mensaje del diagrama: calcularPago(costoDia, dias)
    public void CalcularPago() => MontoPagar = CostoDia * Dias;
}
