using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClasesController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IQrService _qrService;
    private readonly IConfiguration _configuration;

    public ClasesController(IDataService dataService, IQrService qrService, IConfiguration configuration)
    {
        _dataService = dataService;
        _qrService = qrService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClaseCreateDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Asignatura))
            {
                return BadRequest(new { message = "Asignatura es requerida" });
            }
            
            var clase = await _dataService.CreateClaseAsync(dto);
            return Ok(clase);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var clases = await _dataService.GetClasesAsync();

            // Si es Admin, devolver todas las clases
            if (IsAdmin())
            {
                return Ok(clases ?? new List<Clase>());
            }

            // Si es Docente, filtrar solo sus clases
            var userId = GetCurrentUserId();
            var misClases = clases.Where(c => c.DocenteId == userId).ToList();
            return Ok(misClases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener clases: " + ex.Message });
        }
    }

    [HttpGet("mis-clases")]
    public async Task<IActionResult> GetMisClases()
    {
        try
        {
            var todasClases = await _dataService.GetClasesAsync();

            // Si es Admin, devolver todas las clases
            if (IsAdmin())
            {
                return Ok(todasClases ?? new List<Clase>());
            }

            // Si es Docente, filtrar solo sus clases
            var userId = GetCurrentUserId();
            var misClases = todasClases.Where(c => c.DocenteId == userId).ToList();

            return Ok(misClases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener mis clases: " + ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var clase = await _dataService.GetClaseAsync(id);
            if (clase == null)
                return NotFound($"Clase con ID {id} no encontrada");

            // Verificar acceso
            var userId = GetCurrentUserId();
            if (!await TieneAccesoClase(id, userId))
            {
                return Forbid();
            }

            return Ok(clase);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener la clase: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClaseCreateDto dto)
    {
        // Verificar acceso
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(id, userId))
        {
            return Forbid();
        }

        var updated = await _dataService.UpdateClaseAsync(id, dto);
        if (!updated)
            return NotFound();

        var clase = await _dataService.GetClaseAsync(id);
        return Ok(clase);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Verificar acceso
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(id, userId))
        {
            return Forbid();
        }

        var deleted = await _dataService.DeleteClaseAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id)
    {
        // Verificar acceso
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(id, userId))
        {
            return Forbid();
        }

        var clase = await _dataService.GetClaseAsync(id);
        if (clase == null)
            return NotFound();

        if (clase.FinUtc.HasValue)
            return BadRequest("La clase ya está cerrada.");

        await _dataService.CerrarClaseAsync(id);
        return Ok(clase);
    }

    [HttpPost("{id}/reabrir")]
    public async Task<IActionResult> Reabrir(int id)
    {
        // Verificar acceso
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(id, userId))
        {
            return Forbid();
        }

        var result = await _dataService.ReabrirClaseAsync(id);
        if (!result)
            return BadRequest("No se pudo reabrir la clase.");

        var clase = await _dataService.GetClaseAsync(id);
        return Ok(clase);
    }

    [HttpPost("{id}/duplicar")]
    public async Task<IActionResult> Duplicar(int id)
    {
        try
        {
            // Verificar acceso
            var userId = GetCurrentUserId();
            if (!await TieneAccesoClase(id, userId))
            {
                return Forbid();
            }

            var claseNueva = await _dataService.DuplicarClaseAsync(id);
            return Ok(claseNueva);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/qr")]
    public async Task<IActionResult> GetQr(int id)
    {
        try
        {
            // Verificar acceso
            var userId = GetCurrentUserId();
            if (!await TieneAccesoClase(id, userId))
            {
                return Forbid();
            }

            var clase = await _dataService.GetClaseAsync(id);
            if (clase == null)
            {
                return BadRequest(new { message = $"La clase {id} no existe" });
            }

            if (!clase.Activa)
            {
                return BadRequest(new { message = $"La clase {id} ({clase.Asignatura}) no está activa. Fue cerrada el {clase.FinUtc}." });
            }

            var nonce = await _dataService.GenerarTokenClaseAsync(id);
            var publicBaseUrl = _configuration["PublicBaseUrl"];
            var host = !string.IsNullOrWhiteSpace(publicBaseUrl) ? publicBaseUrl : $"{Request.Scheme}://{Request.Host}";
            var payloadUrl = $"{host}/scan?claseId={id}&nonce={nonce}";
            var base64 = _qrService.GenerateBase64Qr(payloadUrl);
            var expiraUtc = DateTime.UtcNow.AddSeconds(300);
            
            // Log para debugging
            Console.WriteLine($"🌐 QR URL generada: {payloadUrl}");
            Console.WriteLine($"🌐 Host usado: {host}");
            Console.WriteLine($"🌐 PublicBaseUrl config: {publicBaseUrl}");

            return Ok(new 
            { 
                base64Png = base64, 
                expiraUtc = expiraUtc, 
                url = payloadUrl,
                claseInfo = new 
                {
                    id = clase.Id,
                    asignatura = clase.Asignatura,
                    inicio = clase.InicioUtc
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error generando QR: {ex.Message}" });
        }
    }

    [HttpGet("{id}/qr.png")]
    public async Task<IActionResult> GetQrPng(int id)
    {
        // Verificar acceso
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(id, userId))
        {
            return Forbid();
        }

        var clase = await _dataService.GetClaseAsync(id);
        if (clase == null)
            return NotFound();

        if (!clase.Activa)
            return BadRequest(new { message = "Clase no activa." });

        var nonce = await _dataService.GenerarTokenClaseAsync(id);
        var publicBaseUrl = _configuration["PublicBaseUrl"];
        var host = !string.IsNullOrWhiteSpace(publicBaseUrl) ? publicBaseUrl : $"{Request.Scheme}://{Request.Host}";
        var payloadUrl = $"{host}/scan?claseId={id}&nonce={nonce}";
        var bytes = _qrService.GeneratePngBytes(payloadUrl);

        return File(bytes, "image/png", $"qr-clase-{id}.png");
    }

    /// <summary>
    /// Endpoint público para obtener información básica de una clase (sin autenticación)
    /// Usado por la página de scan para mostrar información a estudiantes no logueados
    /// </summary>
    [HttpGet("public/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicClassInfo(int id)
    {
        try
        {
            var clase = await _dataService.GetClaseAsync(id);
            if (clase == null)
                return NotFound(new { message = $"Clase con ID {id} no encontrada" });

            // Retornar solo información pública de la clase
            var publicInfo = new
            {
                id = clase.Id,
                asignatura = clase.Asignatura,
                inicioUtc = clase.InicioUtc,
                finUtc = clase.FinUtc,
                activa = clase.Activa,
                descripcion = clase.Descripcion,
                ramo = clase.Ramo != null ? new
                {
                    id = clase.Ramo.Id,
                    nombre = clase.Ramo.Nombre,
                    codigo = clase.Ramo.Codigo,
                    curso = clase.Ramo.Curso != null ? new
                    {
                        id = clase.Ramo.Curso.Id,
                        nombre = clase.Ramo.Curso.Nombre,
                        codigo = clase.Ramo.Curso.Codigo
                    } : null
                } : null
            };

            return Ok(publicInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener información de la clase: " + ex.Message });
        }
    }

    // Método helper para obtener el ID del usuario actual
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }

    // Método helper para verificar si el usuario es Admin
    private bool IsAdmin()
    {
        return User.IsInRole("Administrador");
    }

    // Método helper para verificar si el usuario tiene acceso a una clase
    private async Task<bool> TieneAccesoClase(int claseId, int docenteId)
    {
        // Si es Admin, tiene acceso a todas las clases
        if (IsAdmin())
        {
            return true;
        }

        // Si es Docente, solo tiene acceso a sus propias clases
        var clase = await _dataService.GetClaseAsync(claseId);
        return clase != null && clase.DocenteId == docenteId;
    }
}