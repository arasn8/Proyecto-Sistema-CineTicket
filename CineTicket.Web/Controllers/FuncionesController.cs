using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class FuncionesController : Controller
{
    private readonly CineTicketContext _db;
    private const int MAX_DIAS_RANGO = 60;
    private const int TAM_PAGINA = 15;

    public FuncionesController(CineTicketContext db) => _db = db;

    // Listado con filtros, para no tener una lista interminable
    public async Task<IActionResult> Index(int? idPelicula, int? idSala, DateOnly? fecha, bool soloProximas = true, int page = 1)
    {
        var query = _db.Funciones
            .Include(f => f.IdPeliculaNavigation)
            .Include(f => f.IdSalaNavigation)
            .AsQueryable();

        if (soloProximas)
            query = query.Where(f => f.Fecha >= DateOnly.FromDateTime(DateTime.Today));

        if (idPelicula.HasValue)
            query = query.Where(f => f.IdPelicula == idPelicula.Value);

        if (idSala.HasValue)
            query = query.Where(f => f.IdSala == idSala.Value);

        if (fecha.HasValue)
            query = query.Where(f => f.Fecha == fecha.Value);

        query = query.OrderBy(f => f.Fecha).ThenBy(f => f.Hora);

        int totalRegistros = await query.CountAsync();
        int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalRegistros / (double)TAM_PAGINA));
        page = Math.Max(1, Math.Min(page, totalPaginas));

        var lista = await query.Skip((page - 1) * TAM_PAGINA).Take(TAM_PAGINA).ToListAsync();

        ViewBag.Peliculas = await _db.Peliculas.OrderBy(p => p.Titulo).ToListAsync();
        ViewBag.Salas = await _db.Salas.OrderBy(s => s.Nombre).ToListAsync();
        ViewBag.IdPeliculaSel = idPelicula;
        ViewBag.IdSalaSel = idSala;
        ViewBag.FechaSel = fecha?.ToString("yyyy-MM-dd");
        ViewBag.SoloProximas = soloProximas;
        ViewBag.PaginaActual = page;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.TotalRegistros = totalRegistros;

        return View(lista);
    }

    // Paso 1: elegir pelicula y rango de fechas
    public IActionResult Create()
    {
        CargarListas();
        return View(new FuncionRangoViewModel
        {
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            FechaFin = DateOnly.FromDateTime(DateTime.Today).AddDays(6)
        });
    }

    // Paso 2: recibe el calendario ya armado, con sala y horarios propios por cada dia
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int idPelicula, List<FuncionDiaInputViewModel> dias)
    {
        if (dias == null || !dias.Any())
        {
            TempData["Error"] = "No se recibió ningún día para programar.";
            return RedirectToAction(nameof(Create));
        }

        int creadas = 0, omitidas = 0, diasVacios = 0;

        foreach (var dia in dias)
        {
            var horariosValidos = (dia.Horarios ?? new List<TimeOnly?>())
                .Where(h => h.HasValue).Select(h => h!.Value).Distinct().ToList();

            if (!dia.IdSala.HasValue || !dia.PrecioEntrada.HasValue || !horariosValidos.Any())
            {
                diasVacios++;
                continue; // dia dejado en blanco a proposito: se omite sin error
            }

            foreach (var hora in horariosValidos)
            {
                bool cruce = await _db.Funciones.AnyAsync(f =>
                    f.IdSala == dia.IdSala.Value && f.Fecha == dia.Fecha && f.Hora == hora);

                if (cruce) { omitidas++; continue; }

                _db.Funciones.Add(new Funcione
                {
                    IdPelicula = idPelicula,
                    IdSala = dia.IdSala.Value,
                    Fecha = dia.Fecha,
                    Hora = hora,
                    PrecioEntrada = dia.PrecioEntrada.Value
                });
                creadas++;
            }
        }

        await _db.SaveChangesAsync();

        if (creadas == 0)
        {
            TempData["Error"] = "No se programó ninguna función. Revisa que hayas indicado sala, horario y precio para al menos un día.";
        }
        else
        {
            TempData["Ok"] = $"Se programaron {creadas} funciones." +
                (omitidas > 0 ? $" Se omitieron {omitidas} por cruce de horario." : "") +
                (diasVacios > 0 ? $" ({diasVacios} día(s) se dejaron sin programar)." : "");
        }

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

        bool cruce = await _db.Funciones.AnyAsync(f =>
            f.IdFuncion != model.IdFuncion && f.IdSala == model.IdSala && f.Fecha == model.Fecha && f.Hora == model.Hora);
        if (cruce) ModelState.AddModelError("", "Ya existe otra función en esa sala, fecha y hora.");

        if (!ModelState.IsValid) { CargarListas(); return View(model); }

        _db.Funciones.Update(model);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Función actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
        var funcion = await _db.Funciones.FindAsync(id);
        if (funcion == null) return Json(new { success = false, mensaje = "Función no encontrada." });

        bool tieneVentas = await _db.DetalleVenta.AnyAsync(d => d.IdFuncion == id);
        if (tieneVentas) return Json(new { success = false, mensaje = "No se puede eliminar: ya tiene entradas vendidas." });

        _db.Funciones.Remove(funcion);
        await _db.SaveChangesAsync();
        return Json(new { success = true, mensaje = "Función eliminada." });
    }

    private void CargarListas()
    {
        ViewBag.Peliculas = _db.Peliculas.Where(p => p.Estado).OrderBy(p => p.Titulo).ToList();
        ViewBag.Salas = _db.Salas.OrderBy(s => s.Nombre).ToList();
    }
}