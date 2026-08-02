using CineTicket.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

public class CarteleraController : Controller
{
    private readonly CineTicketContext _db;
    public CarteleraController(CineTicketContext db) => _db = db;

    // Cartelera con filtro  por género
    public async Task<IActionResult> Index(int? idGenero)
    {
        var query = _db.Peliculas.Include(p => p.IdGeneroNavigation).Where(p => p.Estado).AsQueryable();

        if (idGenero.HasValue)
            query = query.Where(p => p.IdGenero == idGenero.Value);

        ViewBag.Generos = await _db.Generos.OrderBy(g => g.Nombre).ToListAsync();
        ViewBag.GeneroSeleccionado = idGenero;

        return View(await query.OrderBy(p => p.Titulo).ToListAsync());
    }

    // Detalle de la película + funciones disponibles
    public async Task<IActionResult> Detalle(int id)
    {
        var pelicula = await _db.Peliculas.Include(p => p.IdGeneroNavigation)
            .FirstOrDefaultAsync(p => p.IdPelicula == id);
        if (pelicula == null) return NotFound();

        ViewBag.Funciones = await _db.Funciones
            .Include(f => f.IdSalaNavigation)
            .Where(f => f.IdPelicula == id && f.Fecha >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(f => f.Fecha).ThenBy(f => f.Hora)
            .ToListAsync();

        return View(pelicula);
    }

    // Mapa de asientos de una función
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