using Microsoft.EntityFrameworkCore;
using registroAsistencia.Models;

namespace registroAsistencia.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Clase> Clases { get; set; }
    public DbSet<Asistencia> Asistencias { get; set; }
    public DbSet<QrClaseToken> QrClaseTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.QrAlumnoBase64).HasColumnType("text");
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        modelBuilder.Entity<Clase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Asignatura).IsRequired().HasMaxLength(200);
            entity.Property(e => e.InicioUtc).IsRequired();
            entity.Property(e => e.FinUtc);
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlumnoId).IsRequired();
            entity.Property(e => e.ClaseId).IsRequired();
            entity.Property(e => e.MarcadaUtc).IsRequired();
            entity.Property(e => e.Metodo).IsRequired().HasMaxLength(50);
            
            entity.HasIndex(e => new { e.AlumnoId, e.ClaseId }).IsUnique();
            
            entity.HasOne<Alumno>()
                .WithMany()
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne<Clase>()
                .WithMany()
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QrClaseToken>(entity =>
        {
            entity.HasKey(e => e.Nonce);
            entity.Property(e => e.Nonce).HasMaxLength(32);
            entity.Property(e => e.ClaseId).IsRequired();
            entity.Property(e => e.ExpiraUtc).IsRequired();
            
            entity.HasOne<Clase>()
                .WithMany()
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}