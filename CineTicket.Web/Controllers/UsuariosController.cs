using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly CineTicketContext _db;
    public UsuariosController(CineTicketContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Usuarios.Include(u => u.IdRolNavigation).OrderBy(u => u.Apellidos).ToListAsync());

    public IActionResult Create()
    {
        ViewBag.Roles = _db.Roles.ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Usuario model)
    {
        ModelState.Remove("IdRolNavigation");
        ModelState.Remove("Venta");
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _db.Roles.ToList();
            return View(model);
        }

        string claveHash = BCrypt.Net.BCrypt.HashPassword(model.Clave);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Usuarios_Insertar @Nombres={model.Nombres}, @Apellidos={model.Apellidos},
                @Correo={model.Correo}, @Clave={claveHash}, @IdRol={model.IdRol}");

        TempData["Ok"] = "Usuario creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();
        ViewBag.Roles = _db.Roles.ToList();
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Usuario model)
    {
        if (id != model.IdUsuario) return NotFound();
        ModelState.Remove("IdRolNavigation");
        ModelState.Remove("Venta");
        ModelState.Remove("Clave");
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _db.Roles.ToList();
            return View(model);
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_Usuarios_Actualizar @IdUsuario={model.IdUsuario}, @Nombres={model.Nombres},
                @Apellidos={model.Apellidos}, @Correo={model.Correo}, @IdRol={model.IdRol}, @Estado={model.Estado}");

        TempData["Ok"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // Eliminacion via AJAX (sin recargar pagina) - satisface el criterio de front-end
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_Usuarios_Eliminar @IdUsuario={id}");
        return Json(new { success = true, mensaje = "Usuario desactivado." });
    }
}