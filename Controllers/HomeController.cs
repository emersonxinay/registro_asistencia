using Microsoft.AspNetCore.Mvc;

namespace registroAsistencia.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View("Dashboard");
    }

    public IActionResult Dashboard()
    {
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
    public IActionResult Asistencias(int? claseId)
    {
        ViewBag.ClaseId = claseId;
        return View();
    }

    [Route("docente/scanner")]
    public IActionResult DocenteScanner()
    {
        Console.WriteLine($"👨‍🏫 ACCESO A SCANNER DOCENTE");
        Console.WriteLine($"👨‍🏫 User-Agent: {Request.Headers.UserAgent}");
        
        Response.ContentType = "text/html; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        
        return View("DocenteScanner");
    }
}