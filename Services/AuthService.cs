using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace registroAsistencia.Services;

public interface IAuthService
{
    Task<Usuario?> LoginAsync(string email, string password);
    Task<Usuario> RegisterAsync(string nombre, string email, string codigoDocente, string password, string? departamento = null, bool esAdministrador = false);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> CodigoDocenteExistsAsync(string codigoDocente);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario);
    Task<Usuario?> GetUsuarioByIdAsync(int id);
    Task UpdateUsuarioAsync(int id, string nombre, string email, string? departamento);
    Task<bool> CambiarPasswordAsync(int id, string passwordActual, string passwordNuevo);
    Task<List<Usuario>> GetAllUsuariosAsync();
    Task CambiarPasswordAdminAsync(int usuarioId, string nuevaPassword);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> LoginAsync(string email, string password)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);
            
        if (usuario == null || !VerifyPassword(password, usuario.PasswordHash))
        {
            return null;
        }

        // Actualizar último acceso
        usuario.UltimoAccesoUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<Usuario> RegisterAsync(string nombre, string email, string codigoDocente, string password, string? departamento = null, bool esAdministrador = false)
    {
        if (await EmailExistsAsync(email))
        {
            throw new InvalidOperationException("El email ya está registrado");
        }

        if (await CodigoDocenteExistsAsync(codigoDocente))
        {
            throw new InvalidOperationException("El código de docente ya está registrado");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Email = email,
            CodigoDocente = codigoDocente,
            PasswordHash = HashPassword(password),
            Departamento = departamento,
            EsAdministrador = esAdministrador,
            Activo = true,
            CreadoUtc = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Usuarios.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> CodigoDocenteExistsAsync(string codigoDocente)
    {
        return await _context.Usuarios.AnyAsync(u => u.CodigoDocente == codigoDocente);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "QuantumAttend_Salt_2025"));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }

    public ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("CodigoDocente", usuario.CodigoDocente),
            new Claim("Departamento", usuario.Departamento ?? ""),
            new Claim(ClaimTypes.Role, usuario.EsAdministrador ? "Administrador" : "Docente")
        };

        var identity = new ClaimsIdentity(claims, "QuantumAttend");
        return new ClaimsPrincipal(identity);
    }

    public async Task<Usuario?> GetUsuarioByIdAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task UpdateUsuarioAsync(int id, string nombre, string email, string? departamento)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        // Verificar si el email cambió y ya existe
        if (usuario.Email != email && await _context.Usuarios.AnyAsync(u => u.Email == email && u.Id != id))
        {
            throw new InvalidOperationException("El email ya está registrado");
        }

        usuario.Nombre = nombre;
        usuario.Email = email;
        usuario.Departamento = departamento;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CambiarPasswordAsync(int id, string passwordActual, string passwordNuevo)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        if (!VerifyPassword(passwordActual, usuario.PasswordHash))
        {
            return false;
        }

        usuario.PasswordHash = HashPassword(passwordNuevo);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<Usuario>> GetAllUsuariosAsync()
    {
        return await _context.Usuarios
            .OrderByDescending(u => u.CreadoUtc)
            .ToListAsync();
    }

    public async Task CambiarPasswordAdminAsync(int usuarioId, string nuevaPassword)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        usuario.PasswordHash = HashPassword(nuevaPassword);
        await _context.SaveChangesAsync();
    }
}