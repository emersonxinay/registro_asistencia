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
            // Generar código de docente automáticamente si no se proporcionó
            string codigoDocente = model.CodigoDocente;
            if (string.IsNullOrWhiteSpace(codigoDocente))
            {
                // Formato: DOC-{timestamp}-{random}
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var random = new Random().Next(1000, 9999);
                codigoDocente = $"DOC-{timestamp}-{random}";
            }

            var usuario = await _authService.RegisterAsync(
                model.Nombre,
                model.Email,
                codigoDocente,
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

    [HttpGet]
    [Route("perfil/editar")]
    [Authorize]
    public async Task<IActionResult> EditarPerfil()
    {
        ViewData["Title"] = "Editar Perfil";

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var usuario = await _authService.GetUsuarioByIdAsync(userId);

        if (usuario == null)
        {
            return NotFound();
        }

        var model = new EditarPerfilViewModel
        {
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            CodigoDocente = usuario.CodigoDocente,
            Departamento = usuario.Departamento
        };

        return View(model);
    }

    [HttpPost]
    [Route("perfil/editar")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPerfil(EditarPerfilViewModel model, string? PasswordActual, string? PasswordNuevo, string? ConfirmarPasswordNuevo)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar Perfil";
            return View(model);
        }

        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Actualizar información básica del perfil
            await _authService.UpdateUsuarioAsync(userId, model.Nombre, model.Email, model.Departamento);

            // Si el usuario está intentando cambiar la contraseña
            if (!string.IsNullOrEmpty(PasswordActual) || !string.IsNullOrEmpty(PasswordNuevo) || !string.IsNullOrEmpty(ConfirmarPasswordNuevo))
            {
                // Validar que todos los campos de contraseña estén completos
                if (string.IsNullOrEmpty(PasswordActual))
                {
                    ModelState.AddModelError("", "Debes ingresar tu contraseña actual");
                    ViewData["Title"] = "Editar Perfil";
                    return View(model);
                }

                if (string.IsNullOrEmpty(PasswordNuevo))
                {
                    ModelState.AddModelError("", "Debes ingresar una nueva contraseña");
                    ViewData["Title"] = "Editar Perfil";
                    return View(model);
                }

                if (PasswordNuevo.Length < 6)
                {
                    ModelState.AddModelError("", "La nueva contraseña debe tener al menos 6 caracteres");
                    ViewData["Title"] = "Editar Perfil";
                    return View(model);
                }

                if (PasswordNuevo != ConfirmarPasswordNuevo)
                {
                    ModelState.AddModelError("", "Las contraseñas no coinciden");
                    ViewData["Title"] = "Editar Perfil";
                    return View(model);
                }

                // Intentar cambiar la contraseña
                var passwordCambiado = await _authService.CambiarPasswordAsync(userId, PasswordActual, PasswordNuevo);

                if (!passwordCambiado)
                {
                    ModelState.AddModelError("", "La contraseña actual es incorrecta");
                    ViewData["Title"] = "Editar Perfil";
                    return View(model);
                }

                TempData["SuccessMessage"] = "Perfil y contraseña actualizados exitosamente. Por favor, inicia sesión nuevamente.";
            }
            else
            {
                TempData["SuccessMessage"] = "Perfil actualizado exitosamente. Por favor, inicia sesión nuevamente para ver los cambios.";
            }

            // Cerrar sesión para actualizar claims
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al actualizar perfil: {ex.Message}");
            ViewData["Title"] = "Editar Perfil";
            return View(model);
        }
    }

    [HttpPost]
    [Route("perfil/cambiar-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos inválidos";
            return RedirectToAction("Perfil");
        }

        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _authService.CambiarPasswordAsync(userId, model.PasswordActual, model.PasswordNuevo);

            if (success)
            {
                TempData["SuccessMessage"] = "Contraseña cambiada exitosamente";
            }
            else
            {
                TempData["ErrorMessage"] = "La contraseña actual es incorrecta";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al cambiar contraseña: {ex.Message}";
        }

        return RedirectToAction("Perfil");
    }

    // ADMIN: Gestión de Usuarios
    [HttpGet]
    [Route("admin/usuarios")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GestionarUsuarios()
    {
        ViewData["Title"] = "Gestión de Usuarios";
        var usuarios = await _authService.GetAllUsuariosAsync();
        return View(usuarios);
    }

    [HttpPost]
    [Route("admin/usuarios/{id}/cambiar-password")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPasswordUsuario(int id, string nuevaPassword)
    {
        if (string.IsNullOrEmpty(nuevaPassword) || nuevaPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "La contraseña debe tener al menos 6 caracteres";
            return RedirectToAction("GestionarUsuarios");
        }

        try
        {
            await _authService.CambiarPasswordAdminAsync(id, nuevaPassword);
            TempData["SuccessMessage"] = "Contraseña actualizada exitosamente";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al cambiar contraseña: {ex.Message}";
        }

        return RedirectToAction("GestionarUsuarios");
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

    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    [Display(Name = "Código de Docente")]
    public string? CodigoDocente { get; set; }

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

public class EditarPerfilViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
    public string Email { get; set; } = "";

    [Display(Name = "Código de Docente")]
    public string CodigoDocente { get; set; } = "";

    [StringLength(255, ErrorMessage = "El departamento no puede exceder 255 caracteres")]
    public string? Departamento { get; set; }
}

public class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Actual")]
    public string PasswordActual { get; set; } = "";

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string PasswordNuevo { get; set; } = "";

    [Required(ErrorMessage = "Confirme la nueva contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("PasswordNuevo", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarPasswordNuevo { get; set; } = "";
}