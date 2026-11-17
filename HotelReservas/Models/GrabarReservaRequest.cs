namespace HotelReservas.Models;

public class GrabarReservaRequest
{
    public string CodCliente { get; set; } = "";
    public DateTime FechaLlegada { get; set; }
    public DateTime FechaSalida { get; set; }
    public List<ItemReserva> Detalle { get; set; } = new();
}

public class ItemReserva
{
    public string NumHabitacion { get; set; } = "";
    public string NombreHuesped { get; set; } = "";
}
