using System.Security.Claims;
using CineTicket.Web.Helpers;
using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace CineTicket.Web.Controllers;

public class CarritoController : Controller
{
    private const string SESSION_KEY = "Carrito";
    private const int MAX_ASIENTOS = 10;
    private readonly CineTicketContext _db;
    public CarritoController(CineTicketContext db) => _db = db;

    // ---------- Ver / modificar el carrito ----------

    public IActionResult Index()
    {
        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);
        ViewBag.Total = carrito.Sum(c => c.Precio);
        return View(carrito);
    }

    public IActionResult Eliminar(int idFuncion, int idAsiento)
    {
        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);
        carrito.RemoveAll(c => c.IdFuncion == idFuncion && c.IdAsiento == idAsiento);
        HttpContext.Session.SetObject(SESSION_KEY, carrito);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult EliminarAjax(int idFuncion, int idAsiento)
    {
        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);
        carrito.RemoveAll(c => c.IdFuncion == idFuncion && c.IdAsiento == idAsiento);
        HttpContext.Session.SetObject(SESSION_KEY, carrito);

        return Json(new { success = true, total = carrito.Sum(c => c.Precio), cantidad = carrito.Count });
    }

    // ---------- Agregar asientos al carrito ----------


    [HttpPost]
    public async Task<IActionResult> Agregar(int idFuncion, int idAsiento)
    {
        var yaVendido = await _db.DetalleVenta.AnyAsync(d => d.IdFuncion == idFuncion && d.IdAsiento == idAsiento);
        if (yaVendido)
        {
            TempData["Error"] = "Ese asiento ya fue vendido.";
            return RedirectToAction("Asientos", "Cartelera", new { idFuncion });
        }

        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);

        if (carrito.Any(c => c.IdFuncion == idFuncion && c.IdAsiento == idAsiento))
        {
            TempData["Error"] = "Ese asiento ya está en tu carrito.";
            return RedirectToAction("Asientos", "Cartelera", new { idFuncion });
        }

        var funcion = await _db.Funciones.Include(f => f.IdPeliculaNavigation).Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == idFuncion);
        var asiento = await _db.Asientos.FindAsync(idAsiento);
        if (funcion == null || asiento == null) return NotFound();

        carrito.Add(new CarritoItem
        {
            IdFuncion = funcion.IdFuncion,
            PeliculaTitulo = funcion.IdPeliculaNavigation.Titulo,
            Fecha = funcion.Fecha,
            Hora = funcion.Hora,
            SalaNombre = funcion.IdSalaNavigation.Nombre,
            IdAsiento = asiento.IdAsiento,
            AsientoNombre = $"{asiento.Fila}{asiento.Numero}",
            Precio = funcion.PrecioEntrada
        });

        HttpContext.Session.SetObject(SESSION_KEY, carrito);
        TempData["Ok"] = "Asiento agregado al carrito.";
        return RedirectToAction("Asientos", "Cartelera", new { idFuncion });
    }

    // Version AJAX (la que usa Cartelera/Asientos.cshtml), con validaciones extra
    [HttpPost]
    public async Task<IActionResult> AgregarAjax(int idFuncion, int idAsiento)
    {
        var funcion = await _db.Funciones.Include(f => f.IdPeliculaNavigation).Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == idFuncion);
        if (funcion == null) return Json(new { success = false, mensaje = "Función no válida." });

        var fechaHoraFuncion = funcion.Fecha.ToDateTime(funcion.Hora);
        if (fechaHoraFuncion < DateTime.Now)
            return Json(new { success = false, mensaje = "Esta función ya pasó, elige otra." });

        var asiento = await _db.Asientos.FindAsync(idAsiento);
        if (asiento == null || asiento.IdSala != funcion.IdSala)
            return Json(new { success = false, mensaje = "Asiento no válido para esta función." });

        var yaVendido = await DisponibilidadHelper.AsientosOcupados(_db, idFuncion).AnyAsync(a => a == idAsiento);
        if (yaVendido) return Json(new { success = false, mensaje = "Ese asiento ya fue vendido." });

        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);
        if (carrito.Any(c => c.IdFuncion == idFuncion && c.IdAsiento == idAsiento))
            return Json(new { success = false, mensaje = "Ese asiento ya está en tu carrito." });

        if (carrito.Count >= MAX_ASIENTOS)
            return Json(new { success = false, mensaje = $"Máximo {MAX_ASIENTOS} asientos por compra." });



        carrito.Add(new CarritoItem
        {
            IdFuncion = funcion.IdFuncion,
            PeliculaTitulo = funcion.IdPeliculaNavigation.Titulo,
            Fecha = funcion.Fecha,
            Hora = funcion.Hora,
            SalaNombre = funcion.IdSalaNavigation.Nombre,
            IdAsiento = asiento.IdAsiento,
            AsientoNombre = $"{asiento.Fila}{asiento.Numero}",
            Precio = funcion.PrecioEntrada
        });

        HttpContext.Session.SetObject(SESSION_KEY, carrito);
        return Json(new { success = true, mensaje = "Asiento agregado al carrito.", totalCarrito = carrito.Count });
    }

    // ---------- Confirmar compra (con estado Pendiente -Confirmada) ----------


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Confirmar()
    {
        var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(SESSION_KEY);
        if (!carrito.Any())
        {
            TempData["Error"] = "Tu carrito está vacío.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var item in carrito)
        {
            bool vendido = await DisponibilidadHelper.AsientosOcupados(_db, item.IdFuncion).AnyAsync(a => a == item.IdAsiento);
            if (vendido)
            {
                TempData["Error"] = $"El asiento {item.AsientoNombre} ya no está disponible, elimínalo del carrito.";
                return RedirectToAction(nameof(Index));
            }
        }

        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        using var transaccion = await _db.Database.BeginTransactionAsync();
        try
        {
            var venta = new Venta
            {
                IdUsuario = idUsuario,
                FechaVenta = DateTime.Now,
                Total = carrito.Sum(c => c.Precio),
                Estado = "PENDIENTE"
            };
            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync();

            foreach (var item in carrito)
            {
                _db.DetalleVenta.Add(new DetalleVentum
                {
                    IdVenta = venta.IdVenta,
                    IdFuncion = item.IdFuncion,
                    IdAsiento = item.IdAsiento,
                    Precio = item.Precio
                });
            }
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            HttpContext.Session.Remove(SESSION_KEY);
            return RedirectToAction(nameof(Espera), new { id = venta.IdVenta });
        }
        catch
        {
            await transaccion.RollbackAsync();
            TempData["Error"] = "Ocurrió un error al procesar tu compra. Intenta nuevamente.";
            return RedirectToAction(nameof(Index));
        }
    }

    // Paso 2: pantalla de espera con el resumen, mientras se "confirma" el pago
    [Authorize]
    public async Task<IActionResult> Espera(int id)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var venta = await CargarVentaDelUsuario(id, idUsuario);
        if (venta == null) return NotFound();

        if (venta.Estado == "CONFIRMADA") return RedirectToAction(nameof(Confirmacion), new { id });
        return View(venta);
    }

    // Simula la confirmacion del pago 
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ConfirmarPago(int id)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var venta = await _db.Ventas.FirstOrDefaultAsync(v => v.IdVenta == id && v.IdUsuario == idUsuario);
        if (venta == null) return NotFound();

        if (venta.Estado == "PENDIENTE")
        {
            venta.Estado = "CONFIRMADA";
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Confirmacion), new { id });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CancelarPendiente(int id)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var venta = await _db.Ventas.Include(v => v.DetalleVenta)
            .FirstOrDefaultAsync(v => v.IdVenta == id && v.IdUsuario == idUsuario && v.Estado == "PENDIENTE");
        if (venta == null) return NotFound();

        _db.DetalleVenta.RemoveRange(venta.DetalleVenta);
        _db.Ventas.Remove(venta);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Compra cancelada.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Confirmacion(int id)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var venta = await CargarVentaDelUsuario(id, idUsuario);
        if (venta == null) return NotFound();

        if (venta.Estado != "CONFIRMADA") return RedirectToAction(nameof(Espera), new { id });
        return View(venta);
    }

    private async Task<Venta?> CargarVentaDelUsuario(int id, int idUsuario)
    {
        return await _db.Ventas
            .Where(v => v.IdVenta == id && v.IdUsuario == idUsuario)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation).ThenInclude(f => f.IdPeliculaNavigation)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation).ThenInclude(f => f.IdSalaNavigation)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdAsientoNavigation)
            .FirstOrDefaultAsync();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CancelarConfirmada(int[] ids)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ventas = await _db.Ventas
            .Where(v => ids.Contains(v.IdVenta) && v.IdUsuario == idUsuario && v.Estado == "CONFIRMADA")
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation)
            .ToListAsync();

        if (!ventas.Any()) return NotFound();

        bool algunaYaPaso = ventas.Any(v => v.DetalleVenta.Any(d =>
            d.IdFuncionNavigation.Fecha.ToDateTime(d.IdFuncionNavigation.Hora) < DateTime.Now));

        if (algunaYaPaso)
        {
            TempData["Error"] = "No se puede cancelar: la función ya pasó.";
            return RedirectToAction("Index", "MisCompras");
        }

        foreach (var venta in ventas)
        {
            _db.DetalleVenta.RemoveRange(venta.DetalleVenta); // libera los asientos
            venta.Estado = "CANCELADA"; // conserva el registro de la venta como historial
        }
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Compra cancelada. Los asientos quedaron disponibles nuevamente.";
        return RedirectToAction("Index", "MisCompras");
    }
}