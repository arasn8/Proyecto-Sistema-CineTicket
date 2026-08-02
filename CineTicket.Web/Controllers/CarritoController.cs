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
    private readonly CineTicketContext _db;
    public CarritoController(CineTicketContext db) => _db = db;

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
            bool vendido = await _db.DetalleVenta.AnyAsync(d => d.IdFuncion == item.IdFuncion && d.IdAsiento == item.IdAsiento);
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
                Estado = "CONFIRMADA"
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
            return RedirectToAction(nameof(Confirmacion), new { id = venta.IdVenta });
        }
        catch
        {
            await transaccion.RollbackAsync();
            TempData["Error"] = "Ocurrió un error al procesar tu compra. Intenta nuevamente.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Confirmacion(int id)
    {
        var venta = await _db.Ventas
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdFuncionNavigation).ThenInclude(f => f.IdPeliculaNavigation)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.IdAsientoNavigation)
            .FirstOrDefaultAsync(v => v.IdVenta == id);
        if (venta == null) return NotFound();
        return View(venta);
    }
}