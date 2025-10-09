using Microsoft.AspNetCore.Mvc;

namespace registroAsistencia.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    [Route("Error/404")]
    [Route("NotFound")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }

    [Route("Error/{statusCode}")]
    public IActionResult Index(int statusCode)
    {
        Response.StatusCode = statusCode;

        if (statusCode == 404)
        {
            return View("NotFound");
        }

        // Para otros errores, puedes crear vistas adicionales
        return View("NotFound");
    }
}
