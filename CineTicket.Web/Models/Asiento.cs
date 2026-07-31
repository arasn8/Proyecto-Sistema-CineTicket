using System;
using System.Collections.Generic;

namespace CineTicket.Web.Models;

public partial class Asiento
{
    public int IdAsiento { get; set; }

    public int IdSala { get; set; }

    public string Fila { get; set; } = null!;

    public int Numero { get; set; }

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Sala IdSalaNavigation { get; set; } = null!;
}
