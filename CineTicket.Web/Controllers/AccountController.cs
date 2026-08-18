using System.Security.Claims;
using CineTicket.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CineTicket.Web.Controllers;

public class AccountController : Controller
{
    private readonly CineTicketContext _db;
    public AccountController(CineTicketContext db) { _db = db; }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string correo, string clave)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Estado);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(clave, usuario.Clave))
        {
            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View();
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
        new Claim(ClaimTypes.Name, usuario.Nombres),
        new Claim(ClaimTypes.Role, usuario.IdRol == 1 ? "Administrador" : "Cliente")
    };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Correo == model.Correo))
            ModelState.AddModelError(nameof(model.Correo), "Ese correo ya está registrado.");

        if (!ModelState.IsValid) return View(model);

        var nuevoUsuario = new Usuario
        {
            Nombres = model.Nombres,
            Apellidos = model.Apellidos,
            Correo = model.Correo,
            Clave = BCrypt.Net.BCrypt.HashPassword(model.Clave),
            IdRol = 2,
            Estado = true
        };

        _db.Usuarios.Add(nuevoUsuario);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Cuenta creada. Ahora puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAjax(string correo, string clave)
    {
        var usuario = await _db.Usuarios.Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Estado);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(clave, usuario.Clave))
            return Json(new { success = false, mensaje = "Correo o contraseña incorrectos." });

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
        new Claim(ClaimTypes.Name, usuario.Nombres),
        new Claim(ClaimTypes.Role, usuario.IdRol == 1 ? "Administrador" : "Cliente")
    };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAjax(RegisterViewModel model)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Correo == model.Correo))
            return Json(new { success = false, mensaje = "Ese correo ya está registrado." });

        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, mensaje = string.Join(" ", errores) });
        }

        var nuevoUsuario = new Usuario
        {
            Nombres = model.Nombres,
            Apellidos = model.Apellidos,
            Correo = model.Correo,
            Clave = BCrypt.Net.BCrypt.HashPassword(model.Clave),
            IdRol = 2,
            Estado = true
        };
        _db.Usuarios.Add(nuevoUsuario);
        await _db.SaveChangesAsync();

        return Json(new { success = true, mensaje = "Cuenta creada. Ahora puedes iniciar sesión." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordAjax(string correo)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo && u.Estado);
        if (usuario == null)
            return Json(new { success = false, mensaje = "No encontramos una cuenta con ese correo." });

        var codigo = new Random().Next(100000, 999999).ToString();
        usuario.CodigoReset = codigo;
        usuario.CodigoResetExpira = DateTime.Now.AddMinutes(15);
        await _db.SaveChangesAsync();

        
        return Json(new { success = true, mensaje = "Código generado (simulado, ya que no hay servidor de correo configurado).", codigoDemo = codigo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordAjax(string correo, string codigo, string nuevaClave)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        if (usuario == null || usuario.CodigoReset != codigo || usuario.CodigoResetExpira < DateTime.Now)
            return Json(new { success = false, mensaje = "Código inválido o expirado." });

        if (string.IsNullOrWhiteSpace(nuevaClave) || nuevaClave.Length < 6)
            return Json(new { success = false, mensaje = "La nueva contraseña debe tener al menos 6 caracteres." });

        usuario.Clave = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
        usuario.CodigoReset = null;
        usuario.CodigoResetExpira = null;
        await _db.SaveChangesAsync();

        return Json(new { success = true, mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }

    public IActionResult AccessDenied() => View();
}