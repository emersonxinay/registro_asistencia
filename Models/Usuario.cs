using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Models;

public class Usuario
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = "";
    
    [Required]
    [StringLength(50)]
    public string CodigoDocente { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    [StringLength(255)]
    public string? Departamento { get; set; }
    
    public bool Activo { get; set; } = true;
    
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime? UltimoAccesoUtc { get; set; }
    
    // Roles simples
    public bool EsAdministrador { get; set; } = false;
    
    // Navegación - clases creadas por este docente
    public virtual ICollection<Clase> Clases { get; set; } = new List<Clase>();
}