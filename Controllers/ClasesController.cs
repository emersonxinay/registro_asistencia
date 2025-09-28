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
            return Ok(clases ?? new List<Clase>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener clases: " + ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var clase = await _dataService.GetClaseAsync(id);
        if (clase == null)
            return NotFound();
        return Ok(clase);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClaseCreateDto dto)
    {
        var updated = await _dataService.UpdateClaseAsync(id, dto);
        if (!updated)
            return NotFound();
        
        var clase = await _dataService.GetClaseAsync(id);
        return Ok(clase);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _dataService.DeleteClaseAsync(id);
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }

    [HttpPost("{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id)
    {
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
            var clase = await _dataService.GetClaseAsync(id);
            if (clase == null)
            {
                return BadRequest($"La clase {id} no existe");
            }
            
            if (!clase.Activa)
            {
                return BadRequest($"La clase {id} ({clase.Asignatura}) no está activa. Fue cerrada el {clase.FinUtc}.");
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
            return BadRequest($"Error generando QR: {ex.Message}");
        }
    }

    [HttpGet("{id}/qr.png")]
    public async Task<IActionResult> GetQrPng(int id)
    {
        var clase = await _dataService.GetClaseAsync(id);
        if (clase == null)
            return NotFound();
        
        if (!clase.Activa)
            return BadRequest("Clase no activa.");

        var nonce = await _dataService.GenerarTokenClaseAsync(id);
        var publicBaseUrl = _configuration["PublicBaseUrl"];
        var host = !string.IsNullOrWhiteSpace(publicBaseUrl) ? publicBaseUrl : $"{Request.Scheme}://{Request.Host}";
        var payloadUrl = $"{host}/scan?claseId={id}&nonce={nonce}";
        var bytes = _qrService.GeneratePngBytes(payloadUrl);

        return File(bytes, "image/png", $"qr-clase-{id}.png");
    }
}