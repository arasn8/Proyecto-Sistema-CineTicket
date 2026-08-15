using System;
using System.Collections.Generic;

namespace CineTicket.Web.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Clave { get; set; } = null!;

    public int IdRol { get; set; }

    public bool Estado { get; set; }

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();

    public string? CodigoReset { get; set; }
    public DateTime? CodigoResetExpira { get; set; }
}
