using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDataService _dataService;

    public DashboardController(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Obtener estadísticas para el dashboard
            var alumnos = await _dataService.GetAlumnosAsync();
            var clases = await _dataService.GetClasesAsync();
            var asistencias = await _dataService.GetAsistenciasAsync();

            // Calcular estadísticas
            var totalAlumnos = alumnos.Count();
            var clasesActivas = clases.Count(c => c.Activa);
            var totalClases = clases.Count();
            
            // Asistencias de hoy
            var hoy = DateTime.Today;
            var asistenciasHoy = asistencias.Count(a => 
                DateTime.Parse(a.MarcadaUtc.ToString()).Date == hoy);

            // Asistencias por clase (top 5)
            var asistenciasPorClase = asistencias
                .GroupBy(a => new { a.ClaseId, a.Asignatura })
                .Select(g => new {
                    ClaseId = g.Key.ClaseId,
                    Asignatura = g.Key.Asignatura,
                    TotalAsistencias = g.Count()
                })
                .OrderByDescending(x => x.TotalAsistencias)
                .Take(5)
                .ToList();

            // Asistencias por día (últimos 7 días)
            var asistenciasPorDia = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(-i))
                .Select(fecha => new {
                    Fecha = fecha.ToString("dd/MM"),
                    Cantidad = asistencias.Count(a => 
                        DateTime.Parse(a.MarcadaUtc.ToString()).Date == fecha)
                })
                .Reverse()
                .ToList();

            var viewModel = new DashboardViewModel
            {
                TotalAlumnos = totalAlumnos,
                ClasesActivas = clasesActivas,
                TotalClases = totalClases,
                AsistenciasHoy = asistenciasHoy,
                AsistenciasPorClase = asistenciasPorClase.Select(x => new ClaseAsistenciaDto
                {
                    ClaseId = x.ClaseId,
                    Asignatura = x.Asignatura ?? "Sin asignatura",
                    TotalAsistencias = x.TotalAsistencias
                }).ToList(),
                AsistenciasPorDia = asistenciasPorDia.Select(x => new DiaAsistenciaDto
                {
                    Fecha = x.Fecha,
                    Cantidad = x.Cantidad
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al cargar el dashboard: " + ex.Message;
            return RedirectToAction("Index", "Home");
        }
    }
}

public class DashboardViewModel
{
    public int TotalAlumnos { get; set; }
    public int ClasesActivas { get; set; }
    public int TotalClases { get; set; }
    public int AsistenciasHoy { get; set; }
    public List<ClaseAsistenciaDto> AsistenciasPorClase { get; set; } = new();
    public List<DiaAsistenciaDto> AsistenciasPorDia { get; set; } = new();
}

public class ClaseAsistenciaDto
{
    public int ClaseId { get; set; }
    public string Asignatura { get; set; } = "";
    public int TotalAsistencias { get; set; }
}

public class DiaAsistenciaDto
{
    public string Fecha { get; set; } = "";
    public int Cantidad { get; set; }
}