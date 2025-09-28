using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Services;
using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [Route("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Home");
        }

        ViewData["Title"] = "Iniciar Sesión";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [Route("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Iniciar Sesión";
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        try
        {
            var usuario = await _authService.LoginAsync(model.Email, model.Password);
            
            if (usuario == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                ViewData["Title"] = "Iniciar Sesión";
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            // Crear claims principal y hacer login
            var claimsPrincipal = _authService.CreateClaimsPrincipal(usuario);
            
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                });

            // Redirigir
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Dashboard", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error en el login: {ex.Message}");
            ViewData["Title"] = "Iniciar Sesión";
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
    }

    [HttpGet]
    [Route("register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Home");
        }

        ViewData["Title"] = "Registro de Docente";
        return View();
    }

    [HttpPost]
    [Route("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Registro de Docente";
            return View(model);
        }

        try
        {
            var usuario = await _authService.RegisterAsync(
                model.Nombre,
                model.Email,
                model.CodigoDocente,
                model.Password,
                model.Departamento,
                false // Los nuevos usuarios no son administradores por defecto
            );

            TempData["SuccessMessage"] = "Registro exitoso. Puedes iniciar sesión ahora.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewData["Title"] = "Registro de Docente";
            return View(model);
        }
    }

    [HttpPost]
    [Route("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Route("perfil")]
    [Authorize]
    public IActionResult Perfil()
    {
        ViewData["Title"] = "Mi Perfil";
        return View();
    }

    // API endpoints for AJAX validation
    [HttpGet]
    [Route("api/auth/check-email")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest();
        }

        var exists = await _authService.EmailExistsAsync(email);
        return Json(new { exists });
    }

    [HttpGet]
    [Route("api/auth/check-codigo")]
    public async Task<IActionResult> CheckCodigoDocente(string codigo)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            return BadRequest();
        }

        var exists = await _authService.CodigoDocenteExistsAsync(codigo);
        return Json(new { exists });
    }
}

// ViewModels
public class LoginViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El código de docente es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    [Display(Name = "Código de Docente")]
    public string CodigoDocente { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirme la contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = "";

    [StringLength(255, ErrorMessage = "El departamento no puede exceder 255 caracteres")]
    public string? Departamento { get; set; }
}