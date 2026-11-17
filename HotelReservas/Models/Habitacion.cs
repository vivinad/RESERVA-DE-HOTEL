namespace HotelReservas.Models;

public class Habitacion
{
    public string NumHabitacion { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal CostoDia { get; set; }
    // Disponible | Reservado | Ocupado
    public string Estado { get; set; } = "Disponible";
}
