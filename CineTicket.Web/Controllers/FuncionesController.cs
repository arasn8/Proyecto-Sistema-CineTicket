using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class FuncionesController : Controller
{
    private readonly CineTicketContext _db;
    public FuncionesController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var lista = await _db.Funciones
            .Include(f => f.IdPeliculaNavigation)
            .Include(f => f.IdSalaNavigation)
            .OrderBy(f => f.Fecha).ThenBy(f => f.Hora)
            .ToListAsync();
        return View(lista);
    }

    public IActionResult Create()
    {
        CargarListas();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Funcione model)
    {
        ModelState.Remove("IdPeliculaNavigation");
        ModelState.Remove("IdSalaNavigation");
        ModelState.Remove("DetalleVenta");

        bool cruce = await _db.Funciones.AnyAsync(f =>
            f.IdSala == model.IdSala && f.Fecha == model.Fecha && f.Hora == model.Hora);
        if (cruce) ModelState.AddModelError("", "Ya existe una función en esa sala, fecha y hora.");

        if (!ModelState.IsValid) { CargarListas(); return View(model); }

        _db.Funciones.Add(model);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Función programada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var funcion = await _db.Funciones.FindAsync(id);
        if (funcion == null) return NotFound();
        CargarListas();
        return View(funcion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Funcione model)
    {
        if (id != model.IdFuncion) return NotFound();
        ModelState.Remove("IdPeliculaNavigation");
        ModelState.Remove("IdSalaNavigation");
        ModelState.Remove("DetalleVenta");
        if (!ModelState.IsValid) { CargarListas(); return View(model); }

        _db.Funciones.Update(model);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Función actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var funcion = await _db.Funciones
            .Include(f => f.IdPeliculaNavigation).Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == id);
        if (funcion == null) return NotFound();
        return View(funcion);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var funcion = await _db.Funciones.FindAsync(id);
        if (funcion != null)
        {
            _db.Funciones.Remove(funcion);
            await _db.SaveChangesAsync();
        }
        TempData["Ok"] = "Función eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private void CargarListas()
    {
        ViewBag.Peliculas = _db.Peliculas.Where(p => p.Estado).OrderBy(p => p.Titulo).ToList();
        ViewBag.Salas = _db.Salas.OrderBy(s => s.Nombre).ToList();
    }
}