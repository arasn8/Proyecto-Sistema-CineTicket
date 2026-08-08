using System.Security.Claims;
using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize]
public class MisComprasController : Controller
{
    private readonly CineTicketContext _db;
    public MisComprasController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ventas = await _db.Ventas
            .Where(v => v.IdUsuario == idUsuario)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation).ThenInclude(f => f.IdPeliculaNavigation)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation).ThenInclude(f => f.IdSalaNavigation)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdAsientoNavigation)
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();

        return View(ventas);
    }
}