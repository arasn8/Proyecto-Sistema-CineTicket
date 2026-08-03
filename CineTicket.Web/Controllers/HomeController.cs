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
            .OrderByDescending(p => p.IdPelicula) // las agregadas mas recientemente = "estrenos"
            .Take(5)
            .ToListAsync();

        return View(estrenos);
    }

    public IActionResult Error() => View();
}