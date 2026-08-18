namespace CineTicket.Web.Models;

// Representa la configuracion de UN dia dentro del calendario de programacion.
// Si IdSala o Horarios quedan vacios, ese dia se omite (no se crea nnguna funcion para el).
public class FuncionDiaInputViewModel
{
    public DateOnly Fecha { get; set; }
    public int? IdSala { get; set; }
    public List<TimeOnly?> Horarios { get; set; } = new();
    public decimal? PrecioEntrada { get; set; }
}