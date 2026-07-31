public class AccountController : Controller
{
    private readonly CineTicketContext _db;
    public AccountController(CineTicketContext db) { _db = db; }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string correo, string clave)
    {
        var usuario = _db.Usuarios.FirstOrDefault(u => u.Correo == correo && u.Clave == clave && u.Estado);
        if (usuario == null)
        {
            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }

        var claims = new List<Claim>
        {
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
}