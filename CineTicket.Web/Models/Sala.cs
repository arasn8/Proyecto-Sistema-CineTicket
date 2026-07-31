using System;
using System.Collections.Generic;

namespace CineTicket.Web.Models;

public partial class Sala
{
    public int IdSala { get; set; }

    public string Nombre { get; set; } = null!;

    public int Capacidad { get; set; }

    public string Tipo { get; set; } = null!;

    public virtual ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();

    public virtual ICollection<Funcione> Funciones { get; set; } = new List<Funcione>();
}
