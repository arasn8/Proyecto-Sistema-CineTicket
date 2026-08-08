using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ReportesController : Controller
{
    private readonly CineTicketContext _db;
    private const int TAM_PAGINA = 10;

    public ReportesController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta, int? idPelicula, int page = 1)
    {
        var query = _db.DetalleVenta
            .Include(d => d.IdVentaNavigation).ThenInclude(v => v.IdUsuarioNavigation)
            .Include(d => d.IdFuncionNavigation).ThenInclude(f => f.IdPeliculaNavigation)
            .Include(d => d.IdAsientoNavigation)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(d => d.IdVentaNavigation.FechaVenta >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(d => d.IdVentaNavigation.FechaVenta <= hasta.Value.AddDays(1));
        if (idPelicula.HasValue)
            query = query.Where(d => d.IdFuncionNavigation.IdPelicula == idPelicula.Value);

        query = query.OrderByDescending(d => d.IdVentaNavigation.FechaVenta);

        int totalRegistros = await query.CountAsync();
        int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalRegistros / (double)TAM_PAGINA));
        page = Math.Max(1, Math.Min(page, totalPaginas));

        var resultados = await query.Skip((page - 1) * TAM_PAGINA).Take(TAM_PAGINA).ToListAsync();
        decimal totalRecaudado = await query.SumAsync(d => (decimal?)d.Precio) ?? 0;

        ViewBag.Peliculas = await _db.Peliculas.OrderBy(p => p.Titulo).ToListAsync();
        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
        ViewBag.IdPelicula = idPelicula;
        ViewBag.PaginaActual = page;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.TotalRegistros = totalRegistros;
        ViewBag.TotalRecaudado = totalRecaudado;

        return View(resultados);
    }
}