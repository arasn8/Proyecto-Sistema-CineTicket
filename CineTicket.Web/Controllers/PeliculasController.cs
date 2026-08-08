using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class PeliculasController : Controller
{
    private readonly CineTicketContext _db;
    public PeliculasController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var lista = await _db.Peliculas.Include(p => p.IdGeneroNavigation)
            .OrderBy(p => p.Titulo).ToListAsync();
        return View(lista);
    }

    public IActionResult Create()
    {
        ViewBag.Generos = _db.Generos.OrderBy(g => g.Nombre).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pelicula model)
    {
        ModelState.Remove("IdGeneroNavigation");
        if (!ModelState.IsValid)
        {
            ViewBag.Generos = _db.Generos.OrderBy(g => g.Nombre).ToList();
            return View(model);
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Peliculas_Insertar
                @Titulo={model.Titulo}, @Sinopsis={model.Sinopsis}, @DuracionMin={model.DuracionMin},
                @Clasificacion={model.Clasificacion}, @IdGenero={model.IdGenero}, @ImagenUrl={model.ImagenUrl}");

        TempData["Ok"] = "Película registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var pelicula = await _db.Peliculas.FindAsync(id);
        if (pelicula == null) return NotFound();
        ViewBag.Generos = _db.Generos.OrderBy(g => g.Nombre).ToList();
        return View(pelicula);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Pelicula model)
    {
        if (id != model.IdPelicula) return NotFound();
        ModelState.Remove("IdGeneroNavigation");
        if (!ModelState.IsValid)
        {
            ViewBag.Generos = _db.Generos.OrderBy(g => g.Nombre).ToList();
            return View(model);
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Peliculas_Actualizar
                @IdPelicula={model.IdPelicula}, @Titulo={model.Titulo}, @Sinopsis={model.Sinopsis},
                @DuracionMin={model.DuracionMin}, @Clasificacion={model.Clasificacion},
                @IdGenero={model.IdGenero}, @ImagenUrl={model.ImagenUrl}");

        TempData["Ok"] = "Película actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var pelicula = await _db.Peliculas.Include(p => p.IdGeneroNavigation)
            .FirstOrDefaultAsync(p => p.IdPelicula == id);
        if (pelicula == null) return NotFound();
        return View(pelicula);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Peliculas_Eliminar @IdPelicula={id}");
        TempData["Ok"] = "Película eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
    await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Peliculas_Eliminar @IdPelicula={id}");
    return Json(new { success = true, mensaje = "Película eliminada correctamente." });
    }
}