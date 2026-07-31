using System;
using System.Collections.Generic;

namespace CineTicket.Web.Models;

public partial class Pelicula
{
    public int IdPelicula { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Sinopsis { get; set; }

    public int DuracionMin { get; set; }

    public string Clasificacion { get; set; } = null!;

    public int IdGenero { get; set; }

    public string? ImagenUrl { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<Funcione> Funciones { get; set; } = new List<Funcione>();

    public virtual Genero IdGeneroNavigation { get; set; } = null!;
}
