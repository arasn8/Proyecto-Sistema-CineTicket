using System.ComponentModel.DataAnnotations;

namespace CineTicket.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Los nombres son obligatorios")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los nombres solo pueden contener letras")]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los apellidos solo pueden contener letras")]
    public string Apellidos { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [DataType(DataType.Password)]
    public string Clave { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña")]
    [DataType(DataType.Password)]
    [Compare("Clave", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarClave { get; set; } = "";
}