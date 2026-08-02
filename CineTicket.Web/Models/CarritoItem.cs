namespace CineTicket.Web.Models;

// solo en la sesión del usuario, no en la base de datos
public class CarritoItem
{
    public int IdFuncion { get; set; }
    public string PeliculaTitulo { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string SalaNombre { get; set; } = "";

    public int IdAsiento { get; set; }
    public string AsientoNombre { get; set; } = ""; // Ej: "A5"

    public decimal Precio { get; set; }
}