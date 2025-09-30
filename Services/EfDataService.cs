using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using System.Security.Claims;

namespace registroAsistencia.Services;

public class EfDataService : IDataService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EfDataService> _logger;
    private readonly IQrService _qrService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EfDataService(ApplicationDbContext context, ILogger<EfDataService> logger, IQrService qrService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _qrService = qrService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Alumno> CreateAlumnoAsync(AlumnoCreateDto dto)
    {
        var alumno = new Alumno
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            QrAlumnoBase64 = ""
        };

        _context.Alumnos.Add(alumno);
        await _context.SaveChangesAsync();
        
        // Generar QR después de guardar para tener el ID con formato JSON para mejor compatibilidad
        var qrPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "student",
            alumnoId = alumno.Id,
            codigo = alumno.Codigo,
            nombre = alumno.Nombre,
            generated = DateTime.UtcNow.ToString("O")
        });
        alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr(qrPayload);
        await _context.SaveChangesAsync();
        
        return alumno;
    }

    public async Task<Alumno?> GetAlumnoAsync(int id)
    {
        return await _context.Alumnos.FindAsync(id);
    }

    public async Task<IEnumerable<Alumno>> GetAlumnosAsync()
    {
        return await _context.Alumnos.ToListAsync();
    }

    public async Task<bool> UpdateAlumnoAsync(int id, AlumnoCreateDto dto)
    {
        var alumno = await _context.Alumnos.FindAsync(id);
        if (alumno == null)
            return false;

        alumno.Codigo = dto.Codigo;
        alumno.Nombre = dto.Nombre;
        // Regenerar QR con los nuevos datos
        var qrPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "student",
            alumnoId = alumno.Id,
            codigo = alumno.Codigo,
            nombre = alumno.Nombre,
            generated = DateTime.UtcNow.ToString("O")
        });
        alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr(qrPayload);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAlumnoQrAsync(int id, string qrBase64)
    {
        var alumno = await _context.Alumnos.FindAsync(id);
        if (alumno == null)
            return false;

        alumno.QrAlumnoBase64 = qrBase64;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAlumnoAsync(int id)
    {
        var alumno = await _context.Alumnos.FindAsync(id);
        if (alumno == null)
            return false;

        _context.Alumnos.Remove(alumno);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Clase> CreateClaseAsync(ClaseCreateDto dto)
    {
        var docenteId = GetCurrentUserId();
        if (docenteId == 0)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        var clase = new Clase
        {
            Asignatura = dto.Asignatura,
            InicioUtc = DateTime.UtcNow,
            DocenteId = docenteId,
            RamoId = dto.RamoId,
            Descripcion = dto.Descripcion
        };

        _context.Clases.Add(clase);
        await _context.SaveChangesAsync();
        return clase;
    }

    private int GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
        }
        return 1; // Fallback al usuario administrador por defecto (admin@quantumattend.edu)
    }

    public async Task<Clase?> GetClaseAsync(int id)
    {
        try
        {
            // Primero intentar con todas las relaciones
            var clase = await _context.Clases
                .Include(c => c.Docente)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clase == null)
                return null;

            // Si tiene RamoId, cargar el Ramo y Curso de forma separada para evitar errores
            if (clase.RamoId.HasValue)
            {
                try
                {
                    var ramoConCurso = await _context.Ramos
                        .Include(r => r.Curso)
                        .FirstOrDefaultAsync(r => r.Id == clase.RamoId.Value);

                    if (ramoConCurso != null)
                    {
                        clase.Ramo = ramoConCurso;
                    }
                }
                catch (Exception ramoEx)
                {
                    Console.WriteLine($"⚠️ Warning: No se pudo cargar Ramo {clase.RamoId}: {ramoEx.Message}");
                    // Continuar sin el Ramo, la clase sigue siendo válida
                }
            }

            return clase;
        }
        catch (Exception ex)
        {
            // Log del error para debugging
            Console.WriteLine($"❌ Error en GetClaseAsync({id}): {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<IEnumerable<Clase>> GetClasesAsync()
    {
        return await _context.Clases
            .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
            .Include(c => c.Docente)
            .OrderByDescending(c => c.InicioUtc)
            .ToListAsync();
    }

    public async Task<bool> UpdateClaseAsync(int id, ClaseCreateDto dto)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null)
            return false;

        clase.Asignatura = dto.Asignatura;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClaseAsync(int id)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null)
            return false;

        _context.Clases.Remove(clase);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CerrarClaseAsync(int id)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null || clase.FinUtc.HasValue)
            return false;

        clase.FinUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReabrirClaseAsync(int id)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null || !clase.FinUtc.HasValue)
            return false;

        clase.FinUtc = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Clase> DuplicarClaseAsync(int id)
    {
        var claseOriginal = await _context.Clases.FindAsync(id);
        if (claseOriginal == null)
            throw new ArgumentException("Clase no encontrada");

        var claseNueva = new Clase
        {
            Asignatura = claseOriginal.Asignatura + " (Copia)",
            InicioUtc = DateTime.UtcNow,
            FinUtc = null
        };

        _context.Clases.Add(claseNueva);
        await _context.SaveChangesAsync();
        return claseNueva;
    }

    public async Task<bool> RegistrarAsistenciaAsync(int alumnoId, int claseId, string metodo)
    {
        var existeAsistencia = await ExisteAsistenciaAsync(alumnoId, claseId);
        if (existeAsistencia)
            return false;

        var clase = await _context.Clases.FindAsync(claseId);
        if (clase == null) return false;

        var ahora = DateTime.UtcNow;
        var minutosRetraso = (int)(ahora - clase.InicioUtc).TotalMinutes;
        var estado = minutosRetraso <= 20 ? EstadoAsistencia.Presente : 
                    (clase.FinUtc == null || ahora <= clase.FinUtc) ? EstadoAsistencia.Tardanza : EstadoAsistencia.Ausente;

        var asistencia = new Asistencia
        {
            AlumnoId = alumnoId,
            ClaseId = claseId,
            MarcadaUtc = ahora,
            ClaseInicioUtc = clase.InicioUtc,
            ClaseFinUtc = clase.FinUtc,
            MinutosRetraso = Math.Max(0, minutosRetraso),
            Estado = estado,
            Metodo = MetodoRegistro.QrEstudiante,
            EsRegistroManual = false
        };

        _context.Asistencias.Add(asistencia);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<dynamic>> GetAsistenciasPorClaseAsync(int claseId)
    {
        try
        {
            var result = await _context.Asistencias
                .Include(a => a.Alumno)
                .Include(a => a.Clase)
                .Where(a => a.ClaseId == claseId)
                .OrderBy(a => a.MarcadaUtc)
                .Select(a => new
                {
                    a.Id,
                    a.ClaseId,
                    Asignatura = a.Clase.Asignatura,
                    a.AlumnoId,
                    AlumnoCodigo = a.Alumno.Codigo,
                    AlumnoNombre = a.Alumno.Nombre,
                    Codigo = a.Alumno.Codigo,
                    Nombre = a.Alumno.Nombre,
                    a.Metodo,
                    a.MarcadaUtc,
                    FechaHoraRegistro = a.MarcadaUtc,
                    a.Estado,
                    a.MinutosRetraso
                })
                .ToListAsync();

            return result.Cast<dynamic>();
        }
        catch (Exception ex)
        {
            // Log del error para debugging
            Console.WriteLine($"❌ Error en GetAsistenciasPorClaseAsync({claseId}): {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<IEnumerable<dynamic>> GetAsistenciasAsync()
    {
        var result = await _context.Asistencias
            .Include(a => a.Alumno)
            .Include(a => a.Clase)
            .OrderBy(a => a.ClaseId).ThenBy(a => a.MarcadaUtc)
            .Select(a => new
            {
                a.Id,
                a.ClaseId,
                Asignatura = a.Clase.Asignatura,
                a.AlumnoId,
                AlumnoCodigo = a.Alumno.Codigo,
                AlumnoNombre = a.Alumno.Nombre,
                Codigo = a.Alumno.Codigo,
                Nombre = a.Alumno.Nombre,
                a.Metodo,
                a.MarcadaUtc,
                FechaHoraRegistro = a.MarcadaUtc,
                a.Estado,
                a.MinutosRetraso
            })
            .ToListAsync();

        return result.Cast<dynamic>();
    }

    public async Task<bool> ExisteAsistenciaAsync(int alumnoId, int claseId)
    {
        return await _context.Asistencias.AnyAsync(a => a.AlumnoId == alumnoId && a.ClaseId == claseId);
    }

    public async Task<string> GenerarTokenClaseAsync(int claseId)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var token = new QrClaseToken
        {
            ClaseId = claseId,
            Nonce = nonce,
            ExpiraUtc = DateTime.UtcNow.AddSeconds(300)
        };

        _context.QrClaseTokens.Add(token);
        await _context.SaveChangesAsync();

        _logger.LogInformation("🎯 Token generado - ClaseId: {ClaseId}, Nonce: {Nonce}, Expira: {ExpiraUtc}",
            claseId, nonce, token.ExpiraUtc);

        return nonce;
    }

    public async Task<bool> ValidarTokenAsync(string nonce, int claseId)
    {
        _logger.LogInformation("🔍 Validando token - Nonce: {Nonce}, ClaseId esperada: {ClaseId}", nonce, claseId);

        var token = await _context.QrClaseTokens.FindAsync(nonce);
        if (token == null)
        {
            _logger.LogWarning("❌ Token no encontrado - Nonce: {Nonce}", nonce);
            return false;
        }

        var now = DateTime.UtcNow;
        var isValidClass = token.ClaseId == claseId;
        var isNotExpired = now <= token.ExpiraUtc;
        var isValid = isValidClass && isNotExpired;

        _logger.LogInformation("📋 Token info - ClaseId: {TokenClase} vs {ClaseEsperada}, Expira: {Expira}, Ahora: {Ahora}, Válido: {Valido}",
            token.ClaseId, claseId, token.ExpiraUtc, now, isValid);

        if (!isValidClass)
        {
            _logger.LogWarning("❌ Token de clase incorrecta - Token ClaseId: {TokenClase}, Esperado: {ClaseEsperada}",
                token.ClaseId, claseId);
        }

        if (!isNotExpired)
        {
            _logger.LogWarning("❌ Token expirado - Expiraba: {ExpiraUtc}, Ahora: {Ahora}", token.ExpiraUtc, now);
        }

        return isValid;
    }

    public async Task ConsumeTokenAsync(string nonce)
    {
        var token = await _context.QrClaseTokens.FindAsync(nonce);
        if (token != null)
        {
            _context.QrClaseTokens.Remove(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("🗑️ Token consumido - Nonce: {Nonce}", nonce);
        }
    }

    // ===== IMPLEMENTACIÓN DE CURSOS =====
    public async Task<Curso> CreateCursoAsync(CursoCreateDto dto)
    {
        var curso = new Curso
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Cursos.Add(curso);
        await _context.SaveChangesAsync();
        return curso;
    }

    public async Task<Curso?> GetCursoAsync(int id)
    {
        return await _context.Cursos
            .Include(c => c.Ramos)
            .Include(c => c.AlumnoCursos)
                .ThenInclude(ac => ac.Alumno)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Curso>> GetCursosAsync()
    {
        return await _context.Cursos
            .Include(c => c.Ramos)
            .Include(c => c.AlumnoCursos)
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<bool> UpdateCursoAsync(int id, CursoUpdateDto dto)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
            return false;

        curso.Nombre = dto.Nombre;
        curso.Codigo = dto.Codigo;
        curso.Descripcion = dto.Descripcion;
        curso.Activo = dto.Activo;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCursoAsync(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
            return false;

        // Soft delete
        curso.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== IMPLEMENTACIÓN DE RAMOS =====
    public async Task<Ramo> CreateRamoAsync(RamoCreateDto dto)
    {
        var ramo = new Ramo
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            CursoId = dto.CursoId,
            Descripcion = dto.Descripcion,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Ramos.Add(ramo);
        await _context.SaveChangesAsync();
        return ramo;
    }

    public async Task<Ramo?> GetRamoAsync(int id)
    {
        return await _context.Ramos
            .Include(r => r.Curso)
            .Include(r => r.Clases)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Ramo>> GetRamosAsync()
    {
        return await _context.Ramos
            .Include(r => r.Curso)
            .Where(r => r.Activo)
            .OrderBy(r => r.Curso.Nombre)
            .ThenBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ramo>> GetRamosByCursoAsync(int cursoId)
    {
        return await _context.Ramos
            .Include(r => r.Curso)
            .Where(r => r.CursoId == cursoId && r.Activo)
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<bool> UpdateRamoAsync(int id, RamoUpdateDto dto)
    {
        var ramo = await _context.Ramos.FindAsync(id);
        if (ramo == null)
            return false;

        ramo.Nombre = dto.Nombre;
        ramo.Codigo = dto.Codigo;
        ramo.Descripcion = dto.Descripcion;
        ramo.Activo = dto.Activo;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRamoAsync(int id)
    {
        var ramo = await _context.Ramos.FindAsync(id);
        if (ramo == null)
            return false;

        // Soft delete
        ramo.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== IMPLEMENTACIÓN DE ALUMNOS-CURSOS =====
    public async Task<AlumnoCurso> InscribirAlumnoEnCursoAsync(AlumnoCursoCreateDto dto)
    {
        // Validar que el alumno no exceda el límite de 2 cursos
        var cursosActuales = await _context.AlumnoCursos
            .Where(ac => ac.AlumnoId == dto.AlumnoId && ac.Activo)
            .CountAsync();

        if (cursosActuales >= 2)
            throw new InvalidOperationException("El alumno ya está inscrito en el máximo de 2 cursos");

        // Verificar que no esté ya inscrito en este curso
        var yaInscrito = await _context.AlumnoCursos
            .AnyAsync(ac => ac.AlumnoId == dto.AlumnoId && ac.CursoId == dto.CursoId && ac.Activo);

        if (yaInscrito)
            throw new InvalidOperationException("El alumno ya está inscrito en este curso");

        var alumnoCurso = new AlumnoCurso
        {
            AlumnoId = dto.AlumnoId,
            CursoId = dto.CursoId,
            FechaInscripcion = DateTime.UtcNow,
            Activo = true
        };

        _context.AlumnoCursos.Add(alumnoCurso);
        await _context.SaveChangesAsync();
        return alumnoCurso;
    }

    public async Task<IEnumerable<AlumnoCursoDto>> GetAlumnosCursoAsync(int cursoId)
    {
        return await _context.AlumnoCursos
            .Include(ac => ac.Alumno)
            .Include(ac => ac.Curso)
            .Where(ac => ac.CursoId == cursoId && ac.Activo)
            .Select(ac => new AlumnoCursoDto(
                ac.Id,
                ac.AlumnoId,
                ac.CursoId,
                ac.Alumno.Nombre,
                ac.Curso.Nombre,
                ac.FechaInscripcion,
                ac.Activo
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<AlumnoCursoDto>> GetCursosAlumnoAsync(int alumnoId)
    {
        return await _context.AlumnoCursos
            .Include(ac => ac.Alumno)
            .Include(ac => ac.Curso)
            .Where(ac => ac.AlumnoId == alumnoId && ac.Activo)
            .Select(ac => new AlumnoCursoDto(
                ac.Id,
                ac.AlumnoId,
                ac.CursoId,
                ac.Alumno.Nombre,
                ac.Curso.Nombre,
                ac.FechaInscripcion,
                ac.Activo
            ))
            .ToListAsync();
    }

    public async Task<bool> DesinscribirAlumnoDelCursoAsync(int alumnoId, int cursoId)
    {
        var alumnoCurso = await _context.AlumnoCursos
            .FirstOrDefaultAsync(ac => ac.AlumnoId == alumnoId && ac.CursoId == cursoId && ac.Activo);

        if (alumnoCurso == null)
            return false;

        alumnoCurso.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AlumnoPerteneceCursoAsync(int alumnoId, int cursoId)
    {
        return await _context.AlumnoCursos
            .AnyAsync(ac => ac.AlumnoId == alumnoId && ac.CursoId == cursoId && ac.Activo);
    }

    public async Task<bool> ValidarLimiteInscripcionAsync(int alumnoId)
    {
        var cursosActuales = await _context.AlumnoCursos
            .Where(ac => ac.AlumnoId == alumnoId && ac.Activo)
            .CountAsync();

        return cursosActuales < 2;
    }
}