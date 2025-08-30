namespace registroAsistencia.Services;

public interface ILoggingService
{
    void LogQrGeneration(int claseId, string nonce, string url);
    void LogTokenValidation(string nonce, int claseId, bool isValid, string reason = "");
    void LogAsistenciaRegistration(int alumnoId, int claseId, string method, bool success, string message = "");
}

public class ConsoleLoggingService : ILoggingService
{
    private readonly ILogger<ConsoleLoggingService> _logger;

    public ConsoleLoggingService(ILogger<ConsoleLoggingService> logger)
    {
        _logger = logger;
    }

    public void LogQrGeneration(int claseId, string nonce, string url)
    {
        _logger.LogInformation("🎯 QR Generated - Clase: {ClaseId}, Nonce: {Nonce}, URL: {Url}", 
            claseId, nonce, url);
    }

    public void LogTokenValidation(string nonce, int claseId, bool isValid, string reason = "")
    {
        if (isValid)
        {
            _logger.LogInformation("✅ Token Valid - Nonce: {Nonce}, Clase: {ClaseId}", nonce, claseId);
        }
        else
        {
            _logger.LogWarning("❌ Token Invalid - Nonce: {Nonce}, Clase: {ClaseId}, Reason: {Reason}", 
                nonce, claseId, reason);
        }
    }

    public void LogAsistenciaRegistration(int alumnoId, int claseId, string method, bool success, string message = "")
    {
        if (success)
        {
            _logger.LogInformation("📝 Asistencia Registered - Alumno: {AlumnoId}, Clase: {ClaseId}, Method: {Method}", 
                alumnoId, claseId, method);
        }
        else
        {
            _logger.LogError("💥 Asistencia Failed - Alumno: {AlumnoId}, Clase: {ClaseId}, Error: {Message}", 
                alumnoId, claseId, message);
        }
    }
}