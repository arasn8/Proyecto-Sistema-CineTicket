using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class GenerosController : Controller
{
    private readonly CineTicketContext _db;
    public GenerosController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Generos.OrderBy(g => g.Nombre).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Genero model)
    {
        ModelState.Remove("Peliculas");
        if (!ModelState.IsValid) return View(model);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_Generos_Insertar @Nombre={model.Nombre}");

        TempData["Ok"] = "Género registrado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var genero = await _db.Generos.FindAsync(id);
        if (genero == null) return NotFound();
        return View(genero);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Genero model)
    {
        if (id != model.IdGenero) return NotFound();
        ModelState.Remove("Peliculas");
        if (!ModelState.IsValid) return View(model);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_Generos_Actualizar @IdGenero={model.IdGenero}, @Nombre={model.Nombre}");

        TempData["Ok"] = "Género actualizado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var genero = await _db.Generos.FindAsync(id);
        if (genero == null) return NotFound();
        return View(genero);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Generos_Eliminar @IdGenero={id}");
        TempData["Ok"] = "Género eliminado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
    await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Generos_Eliminar @IdGenero={id}");
    return Json(new { success = true, mensaje = "Género eliminado." });
    }

}