using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlumnosController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IQrService _qrService;

    public AlumnosController(IDataService dataService, IQrService qrService)
    {
        _dataService = dataService;
        _qrService = qrService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AlumnoCreateDto dto)
    {
        var alumno = await _dataService.CreateAlumnoAsync(dto);
        alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr($"alumno:{alumno.Id}");
        return Ok(alumno);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var alumnos = await _dataService.GetAlumnosAsync();
        return Ok(alumnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var alumno = await _dataService.GetAlumnoAsync(id);
        if (alumno == null)
            return NotFound();
        return Ok(alumno);
    }
}