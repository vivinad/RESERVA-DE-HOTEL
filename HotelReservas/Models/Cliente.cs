namespace HotelReservas.Models;

public class Cliente
{
    public string CodCliente { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
