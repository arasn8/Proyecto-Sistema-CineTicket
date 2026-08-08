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

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Funciones_Insertar @IdPelicula={model.IdPelicula}, @IdSala={model.IdSala},
                @Fecha={model.Fecha.ToDateTime(TimeOnly.MinValue)}, @Hora={model.Hora.ToTimeSpan()}, @PrecioEntrada={model.PrecioEntrada}");

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

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Funciones_Actualizar @IdFuncion={model.IdFuncion}, @IdPelicula={model.IdPelicula},
                @IdSala={model.IdSala}, @Fecha={model.Fecha.ToDateTime(TimeOnly.MinValue)},
                @Hora={model.Hora.ToTimeSpan()}, @PrecioEntrada={model.PrecioEntrada}");

        TempData["Ok"] = "Función actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Funciones_Eliminar @IdFuncion={id}");
        return Json(new { success = true, mensaje = "Función eliminada." });
    }

    private void CargarListas()
    {
        ViewBag.Peliculas = _db.Peliculas.Where(p => p.Estado).OrderBy(p => p.Titulo).ToList();
        ViewBag.Salas = _db.Salas.OrderBy(s => s.Nombre).ToList();
    }
}