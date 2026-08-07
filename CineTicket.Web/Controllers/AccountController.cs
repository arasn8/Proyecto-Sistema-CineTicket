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
        return RedirectToAction("Login");
    }
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string nombres, string apellidos, string correo, string clave)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Correo == correo))
        {
            ViewBag.Error = "Ese correo ya está registrado.";
            return View();
        }

        var nuevoUsuario = new Usuario
        {
            Nombres = nombres,
            Apellidos = apellidos,
            Correo = correo,
           Clave = BCrypt.Net.BCrypt.HashPassword(clave),
            IdRol = 2, // 2 = Cliente 
            Estado = true
        };

        _db.Usuarios.Add(nuevoUsuario);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Cuenta creada. Ahora puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

        public IActionResult AccessDenied() => View();
}