using System.ComponentModel.DataAnnotations;

namespace CineTicket.Web.Models;

public class FuncionRangoViewModel
{
    [Required(ErrorMessage = "Selecciona una película")]
    public int IdPelicula { get; set; }

    [Required(ErrorMessage = "Indica la fecha de inicio")]
    [DataType(DataType.Date)]
    public DateOnly FechaInicio { get; set; }

    [Required(ErrorMessage = "Indica la fecha de fin")]
    [DataType(DataType.Date)]
    public DateOnly FechaFin { get; set; }
}