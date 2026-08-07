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

        model.Clave = BCrypt.Net.BCrypt.HashPassword(model.Clave);
        _db.Usuarios.Add(model);
        await _db.SaveChangesAsync();

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
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _db.Roles.ToList();
            return View(model);
        }

        _db.Usuarios.Update(model);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _db.Usuarios.Include(u => u.IdRolNavigation).FirstOrDefaultAsync(u => u.IdUsuario == id);
        if (usuario == null) return NotFound();
        return View(usuario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario != null)
        {
            usuario.Estado = false; // borrado lógico
            await _db.SaveChangesAsync();
        }
        TempData["Ok"] = "Usuario desactivado.";
        return RedirectToAction(nameof(Index));
    }
}