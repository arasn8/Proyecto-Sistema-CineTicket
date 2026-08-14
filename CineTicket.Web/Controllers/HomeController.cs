using CineTicket.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

public class HomeController : Controller
{
    private readonly CineTicketContext _db;
    public HomeController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var estrenos = await _db.Peliculas
            .Include(p => p.IdGeneroNavigation)
            .Where(p => p.Estado)
            .OrderByDescending(p => p.IdPelicula)
            .Take(5)
            .ToListAsync();

        ViewBag.TodasPeliculas = await _db.Peliculas.Where(p => p.Estado).OrderBy(p => p.Titulo).ToListAsync();

        var topPorVentas = await _db.DetalleVenta
            .Include(d => d.IdFuncionNavigation)
            .Where(d => d.IdVentaNavigation.Estado == "CONFIRMADA")
            .GroupBy(d => d.IdFuncionNavigation.IdPelicula)
            .Select(g => new { IdPelicula = g.Key, Ventas = g.Count() })
            .OrderByDescending(x => x.Ventas)
            .Take(8)
            .ToListAsync();

        var idsTop = topPorVentas.Select(x => x.IdPelicula).ToList();
        var topPeliculas = await _db.Peliculas.Where(p => idsTop.Contains(p.IdPelicula)).ToListAsync();
        var listaTop = idsTop.Select(id => topPeliculas.FirstOrDefault(p => p.IdPelicula == id)).Where(p => p != null).ToList();

        ViewBag.TopPeliculas = listaTop.Any() ? listaTop : estrenos; // si aun no hay ventas, muestra los estrenos

        return View(estrenos);
    }
}