using CineTicket.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

public class CarteleraController : Controller
{
    private readonly CineTicketContext _db;
    public CarteleraController(CineTicketContext db) => _db = db;

    // Cartelera con filtros por genero, formato, idioma y busqueda
    public async Task<IActionResult> Index(int? idGenero, string? tipoSala, string? idioma, string? buscar)
    {
        var query = _db.Peliculas.Include(p => p.IdGeneroNavigation).Where(p => p.Estado).AsQueryable();

        if (idGenero.HasValue)
            query = query.Where(p => p.IdGenero == idGenero.Value);

        if (!string.IsNullOrWhiteSpace(idioma))
            query = query.Where(p => p.Idioma == idioma);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Titulo.Contains(buscar));

        var peliculas = await query.OrderBy(p => p.Titulo).ToListAsync();

        // Formatos de sala disponibles por pelicula (para las etiquetas 2D/3D/VIP) y filtro por tipo de sala
        var formatosPorPelicula = await _db.Funciones
            .Where(f => f.Fecha >= DateOnly.FromDateTime(DateTime.Today))
            .Include(f => f.IdSalaNavigation)
            .GroupBy(f => f.IdPelicula)
            .Select(g => new { IdPelicula = g.Key, Formatos = g.Select(f => f.IdSalaNavigation.Tipo).Distinct().ToList() })
            .ToDictionaryAsync(x => x.IdPelicula, x => x.Formatos);

        if (!string.IsNullOrWhiteSpace(tipoSala))
        {
            var idsConFormato = formatosPorPelicula.Where(kv => kv.Value.Contains(tipoSala)).Select(kv => kv.Key).ToHashSet();
            peliculas = peliculas.Where(p => idsConFormato.Contains(p.IdPelicula)).ToList();
        }

        // Las 5 mas recientemente agregadas se marcan como ESTRENO
        var idsEstreno = (await _db.Peliculas.Where(p => p.Estado).OrderByDescending(p => p.IdPelicula).Take(5).Select(p => p.IdPelicula).ToListAsync()).ToHashSet();

        ViewBag.Generos = await _db.Generos.OrderBy(g => g.Nombre).ToListAsync();
        ViewBag.GeneroSeleccionado = idGenero;
        ViewBag.TipoSalaSeleccionado = tipoSala;
        ViewBag.IdiomaSeleccionado = idioma;
        ViewBag.Buscar = buscar;
        ViewBag.FormatosPorPelicula = formatosPorPelicula;
        ViewBag.IdsEstreno = idsEstreno;

        return View(peliculas);
    }

    // Detalle de la pelicula + funciones disponibles, con calendario de fechas y filtro de formato
    public async Task<IActionResult> Detalle(int id, DateOnly? fecha, string? tipoSala)
    {
        var pelicula = await _db.Peliculas.Include(p => p.IdGeneroNavigation)
            .FirstOrDefaultAsync(p => p.IdPelicula == id);
        if (pelicula == null) return NotFound();

        var todasFunciones = await _db.Funciones
            .Include(f => f.IdSalaNavigation)
            .Where(f => f.IdPelicula == id && f.Fecha >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(f => f.Fecha).ThenBy(f => f.Hora)
            .ToListAsync();

        var fechasDisponibles = todasFunciones.Select(f => f.Fecha).Distinct().OrderBy(f => f).ToList();
        var fechaSeleccionada = fecha ?? fechasDisponibles.FirstOrDefault();

        var funcionesFiltradas = todasFunciones.Where(f => f.Fecha == fechaSeleccionada);
        if (!string.IsNullOrWhiteSpace(tipoSala))
            funcionesFiltradas = funcionesFiltradas.Where(f => f.IdSalaNavigation.Tipo == tipoSala);

        ViewBag.Funciones = funcionesFiltradas.ToList();
        ViewBag.FechasDisponibles = fechasDisponibles;
        ViewBag.FechaSeleccionada = fechaSeleccionada;
        ViewBag.TipoSalaSeleccionado = tipoSala;

        return View(pelicula);
    }

    // Mapa de asientos de una funcion
    public async Task<IActionResult> Asientos(int idFuncion)
    {
        var funcion = await _db.Funciones
            .Include(f => f.IdPeliculaNavigation)
            .Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == idFuncion);
        if (funcion == null) return NotFound();

        var asientosSala = await _db.Asientos
            .Where(a => a.IdSala == funcion.IdSala)
            .OrderBy(a => a.Fila).ThenBy(a => a.Numero)
            .ToListAsync();

        var ocupados = await _db.DetalleVenta
            .Where(d => d.IdFuncion == idFuncion)
            .Select(d => d.IdAsiento)
            .ToListAsync();

        ViewBag.Funcion = funcion;
        ViewBag.Ocupados = ocupados;
        return View(asientosSala);
    }
}