using System;
using System.Collections.Generic;

namespace CineTicket.Web.Models;

public partial class DetalleVentum
{
    public int IdDetalle { get; set; }

    public int IdVenta { get; set; }

    public int IdFuncion { get; set; }

    public int IdAsiento { get; set; }

    public decimal Precio { get; set; }

    public virtual Asiento IdAsientoNavigation { get; set; } = null!;

    public virtual Funcione IdFuncionNavigation { get; set; } = null!;

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}
