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

        // Recuperamos el Id que acaba de generar el procedimiento almacenado
        var salaCreada = await _db.Salas
            .Where(s => s.Nombre == model.Nombre)
            .OrderByDescending(s => s.IdSala)
            .FirstAsync();

        await GenerarAsientosAsync(salaCreada.IdSala, model.Capacidad);

        TempData["Ok"] = $"Sala registrada con {model.Capacidad} asientos generados automáticamente.";
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

        var salaActual = await _db.Salas.AsNoTracking().FirstOrDefaultAsync(s => s.IdSala == id);
        bool cambioCapacidad = salaActual != null && salaActual.Capacidad != model.Capacidad;

        if (cambioCapacidad)
        {
            bool tieneVentas = await _db.DetalleVenta
                .Include(d => d.IdAsientoNavigation)
                .AnyAsync(d => d.IdAsientoNavigation.IdSala == id);

            if (tieneVentas)
            {
                ModelState.AddModelError("", "No se puede cambiar la capacidad: esta sala ya tiene entradas vendidas.");
                return View(model);
            }
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Salas_Actualizar @IdSala={model.IdSala}, @Nombre={model.Nombre},
                @Capacidad={model.Capacidad}, @Tipo={model.Tipo}");

        if (cambioCapacidad)
        {
            var asientosViejos = await _db.Asientos.Where(a => a.IdSala == id).ToListAsync();
            _db.Asientos.RemoveRange(asientosViejos);
            await _db.SaveChangesAsync();

            await GenerarAsientosAsync(id, model.Capacidad);
            TempData["Ok"] = $"Sala actualizada y asientos regenerados ({model.Capacidad} asientos).";
        }
        else
        {
            TempData["Ok"] = "Sala actualizada.";
        }

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

    // Genera asientos en filas de 10 (A1..A10, B1..B10, ...) hasta completar la capacidad
    private async Task GenerarAsientosAsync(int idSala, int capacidad)
    {
        const int asientosPorFila = 10;
        int totalFilas = (int)Math.Ceiling(capacidad / (double)asientosPorFila);
        int contados = 0;
        var nuevosAsientos = new List<Asiento>();

        for (int f = 0; f < totalFilas; f++)
        {
            char letraFila = (char)('A' + f);
            int enEstaFila = Math.Min(asientosPorFila, capacidad - contados);

            for (int n = 1; n <= enEstaFila; n++)
            {
                nuevosAsientos.Add(new Asiento { IdSala = idSala, Fila = letraFila.ToString(), Numero = n });
            }
            contados += enEstaFila;
        }

        _db.Asientos.AddRange(nuevosAsientos);
        await _db.SaveChangesAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
    await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Salas_Eliminar @IdSala={id}");
    return Json(new { success = true, mensaje = "Sala eliminada." });
    }
}