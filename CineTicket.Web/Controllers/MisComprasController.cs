using System.Security.Claims;
using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

public class FuncionCompradaVM
{
    public int IdFuncion { get; set; }
    public string PeliculaTitulo { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string SalaNombre { get; set; } = "";
    public List<string> Asientos { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime UltimaCompra { get; set; }
    public List<int> VentaIds { get; set; } = new();
}

[Authorize]
public class MisComprasController : Controller
{
    private readonly CineTicketContext _db;
    public MisComprasController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        ViewBag.Pendientes = await _db.Ventas
            .Where(v => v.IdUsuario == idUsuario && v.Estado == "PENDIENTE")
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();

        var detalles = await _db.DetalleVenta
            .Where(d => d.IdVentaNavigation.IdUsuario == idUsuario && d.IdVentaNavigation.Estado == "CONFIRMADA")
            .Include(d => d.IdVentaNavigation)
            .Include(d => d.IdFuncionNavigation).ThenInclude(f => f.IdPeliculaNavigation)
            .Include(d => d.IdFuncionNavigation).ThenInclude(f => f.IdSalaNavigation)
            .Include(d => d.IdAsientoNavigation)
            .ToListAsync();

        var agrupado = detalles
            .GroupBy(d => d.IdFuncion)
            .Select(g => new FuncionCompradaVM
            {
                IdFuncion = g.Key,
                PeliculaTitulo = g.First().IdFuncionNavigation.IdPeliculaNavigation.Titulo,
                Fecha = g.First().IdFuncionNavigation.Fecha,
                Hora = g.First().IdFuncionNavigation.Hora,
                SalaNombre = g.First().IdFuncionNavigation.IdSalaNavigation.Nombre,
                Asientos = g.Select(x => $"{x.IdAsientoNavigation.Fila}{x.IdAsientoNavigation.Numero}").OrderBy(x => x).ToList(),
                Total = g.Sum(x => x.Precio),
                UltimaCompra = g.Max(x => x.IdVentaNavigation.FechaVenta),
                VentaIds = g.Select(x => x.IdVenta).Distinct().ToList(),
            })
            .OrderByDescending(x => x.UltimaCompra)
            .ToList();

        return View(agrupado);
    }
}