using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class SalasController : Controller
{
    private readonly CineTicketContext _db;
    public SalasController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Salas.OrderBy(s => s.Nombre).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Sala model)
    {
        ModelState.Remove("Asientos");
        ModelState.Remove("Funciones");
        if (!ModelState.IsValid) return View(model);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Salas_Insertar @Nombre={model.Nombre}, @Capacidad={model.Capacidad}, @Tipo={model.Tipo}");

        TempData["Ok"] = "Sala registrada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sala = await _db.Salas.FindAsync(id);
        if (sala == null) return NotFound();
        return View(sala);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sala model)
    {
        if (id != model.IdSala) return NotFound();
        ModelState.Remove("Asientos");
        ModelState.Remove("Funciones");
        if (!ModelState.IsValid) return View(model);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Salas_Actualizar @IdSala={model.IdSala}, @Nombre={model.Nombre},
                @Capacidad={model.Capacidad}, @Tipo={model.Tipo}");

        TempData["Ok"] = "Sala actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var sala = await _db.Salas.FindAsync(id);
        if (sala == null) return NotFound();
        return View(sala);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Salas_Eliminar @IdSala={id}");
        TempData["Ok"] = "Sala eliminada.";
        return RedirectToAction(nameof(Index));
    }
}