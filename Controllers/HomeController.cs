using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace registroAsistencia.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Bienvenido a Registro Asistencia por QR";
        ViewData["Subtitle"] = "Sistema inteligente de registro de asistencia con tecnología QR";
        return View();
    }

    [Route("dashboard")]
    [Authorize]
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Subtitle"] = "Sistema de registro de asistencia con tecnología QR avanzada";
        return View();
    }

    [Route("scan")]
    public IActionResult Scan(int claseId, string nonce)
    {
        Console.WriteLine($"📱 ACCESO A SCAN - ClaseId: {claseId}, Nonce: {nonce}");
        Console.WriteLine($"📱 User-Agent: {Request.Headers.UserAgent}");
        Console.WriteLine($"📱 Accept: {Request.Headers.Accept}");
        
        // Forzar Content-Type para móviles
        Response.ContentType = "text/html; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        
        ViewBag.ClaseId = claseId;
        ViewBag.Nonce = nonce;
        return View("ScanSimple");
    }

    [Route("asistencias/{claseId:int?}")]
    [Authorize]
    public IActionResult Asistencias(int? claseId)
    {
        ViewBag.ClaseId = claseId;
        return View();
    }

    [Route("asistencias-simple")]
    [Authorize]
    public IActionResult AsistenciasSimple()
    {
        return View();
    }

    [Route("docente/scanner")]
    [Authorize]
    public IActionResult DocenteScanner()
    {
        Console.WriteLine($"👨‍🏫 ACCESO A SCANNER DOCENTE");
        Console.WriteLine($"👨‍🏫 User-Agent: {Request.Headers.UserAgent}");
        
        Response.ContentType = "text/html; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        
        return View("DocenteScanner");
    }

    [Route("alumnos")]
    [Authorize]
    public IActionResult Alumnos()
    {
        ViewData["Title"] = "Gestión de Estudiantes";
        ViewData["Subtitle"] = "Administra estudiantes, genera códigos QR y controla registros";
        return View();
    }

    [Route("clases")]
    [Authorize]
    public IActionResult Clases()
    {
        ViewData["Title"] = "Gestión de Clases";
        ViewData["Subtitle"] = "Crea y administra clases, controla asistencias y genera reportes";
        return View();
    }

    [Route("clases/{id:int}/qr")]
    [Authorize]
    public IActionResult ClaseQr(int id)
    {
        ViewData["Title"] = "Código QR de Clase";
        ViewData["ClaseId"] = id;
        return View("ClaseQr");
    }

    [Route("ayuda")]
    public IActionResult Help()
    {
        ViewData["Title"] = "Ayuda e Instrucciones";
        ViewData["Subtitle"] = "Guía completa para usar Registro Asistencia por QR eficientemente";
        return View();
    }
}