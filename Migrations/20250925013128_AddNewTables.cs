using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace registroAsistencia.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primero agregamos las columnas nuevas
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Asistencias",
                type: "integer",
                nullable: false,
                defaultValue: 1); // EstadoAsistencia.Presente por defecto
            
            // Agregamos columna temporal para el nuevo método
            migrationBuilder.AddColumn<int>(
                name: "MetodoNuevo",
                table: "Asistencias",
                type: "integer",
                nullable: false,
                defaultValue: 1); // MetodoRegistro.QrEstudiante por defecto

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaseFinUtc",
                table: "Asistencias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaseInicioUtc",
                table: "Asistencias",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreadoUtc",
                table: "Asistencias",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DocenteQueRegistroId",
                table: "Asistencias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsRegistroManual",
                table: "Asistencias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Estado ya fue agregado arriba

            migrationBuilder.AddColumn<string>(
                name: "JustificacionManual",
                table: "Asistencias",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinutosRetraso",
                table: "Asistencias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificadoUtc",
                table: "Asistencias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesAsistencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaseId = table.Column<int>(type: "integer", nullable: false),
                    LimitePresenteMinutos = table.Column<int>(type: "integer", nullable: false),
                    PermiteRegistroManual = table.Column<bool>(type: "boolean", nullable: false),
                    NotificarTardanzas = table.Column<bool>(type: "boolean", nullable: false),
                    MarcarAusenteAutomatico = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesAsistencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAsistencia_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocenteCursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    CursoId = table.Column<int>(type: "integer", nullable: false),
                    EsPropietario = table.Column<bool>(type: "boolean", nullable: false),
                    AsignadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocenteCursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocenteCursos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocenteCursos_Usuarios_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocenteRamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    RamoId = table.Column<int>(type: "integer", nullable: false),
                    AsignadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocenteRamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocenteRamos_Ramos_RamoId",
                        column: x => x.RamoId,
                        principalTable: "Ramos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocenteRamos_Usuarios_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorarioClases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RamoId = table.Column<int>(type: "integer", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Aula = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioClases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorarioClases_Ramos_RamoId",
                        column: x => x.RamoId,
                        principalTable: "Ramos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsistenciaId = table.Column<int>(type: "integer", nullable: false),
                    Accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Justificacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DatosAnteriores = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_Asistencias_AsistenciaId",
                        column: x => x.AsistenciaId,
                        principalTable: "Asistencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QrEstudiantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    QrData = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GeneradoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrEstudiantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrEstudiantes_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_DocenteQueRegistroId",
                table: "Asistencias",
                column: "DocenteQueRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAsistencia_ClaseId",
                table: "ConfiguracionesAsistencia",
                column: "ClaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocenteCursos_CursoId",
                table: "DocenteCursos",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocenteCursos_DocenteId_CursoId",
                table: "DocenteCursos",
                columns: new[] { "DocenteId", "CursoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocenteRamos_DocenteId_RamoId",
                table: "DocenteRamos",
                columns: new[] { "DocenteId", "RamoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocenteRamos_RamoId",
                table: "DocenteRamos",
                column: "RamoId");

            migrationBuilder.CreateIndex(
                name: "IX_HorarioClases_RamoId",
                table: "HorarioClases",
                column: "RamoId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_AsistenciaId",
                table: "LogsAuditoria",
                column: "AsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_QrEstudiantes_AlumnoId",
                table: "QrEstudiantes",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_QrEstudiantes_QrData",
                table: "QrEstudiantes",
                column: "QrData",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Usuarios_DocenteQueRegistroId",
                table: "Asistencias",
                column: "DocenteQueRegistroId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            
            // Ahora migramos los datos de Metodo string a MetodoNuevo int
            migrationBuilder.Sql(@"
                UPDATE ""Asistencias"" 
                SET ""MetodoNuevo"" = CASE 
                    WHEN ""Metodo"" = 'QR' THEN 1
                    WHEN ""Metodo"" = 'Manual' THEN 3
                    WHEN ""Metodo"" = 'Scanner' THEN 2
                    ELSE 1
                END;
            ");
            
            // Eliminamos la columna vieja Metodo
            migrationBuilder.DropColumn(
                name: "Metodo",
                table: "Asistencias");
            
            // Renombramos MetodoNuevo a Metodo
            migrationBuilder.RenameColumn(
                name: "MetodoNuevo",
                table: "Asistencias",
                newName: "Metodo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Usuarios_DocenteQueRegistroId",
                table: "Asistencias");

            migrationBuilder.DropTable(
                name: "ConfiguracionesAsistencia");

            migrationBuilder.DropTable(
                name: "DocenteCursos");

            migrationBuilder.DropTable(
                name: "DocenteRamos");

            migrationBuilder.DropTable(
                name: "HorarioClases");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "QrEstudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_DocenteQueRegistroId",
                table: "Asistencias");

            // Revertir la transformación de Metodo
            migrationBuilder.AddColumn<string>(
                name: "MetodoViejo",
                table: "Asistencias",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "QR");
            
            migrationBuilder.Sql(@"
                UPDATE ""Asistencias"" 
                SET ""MetodoViejo"" = CASE 
                    WHEN ""Metodo"" = 1 THEN 'QR'
                    WHEN ""Metodo"" = 2 THEN 'Scanner'
                    WHEN ""Metodo"" = 3 THEN 'Manual'
                    ELSE 'QR'
                END;
            ");
            
            migrationBuilder.DropColumn(
                name: "Metodo",
                table: "Asistencias");
            
            migrationBuilder.RenameColumn(
                name: "MetodoViejo",
                table: "Asistencias",
                newName: "Metodo");

            migrationBuilder.DropColumn(
                name: "ClaseFinUtc",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "ClaseInicioUtc",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "CreadoUtc",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "DocenteQueRegistroId",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "EsRegistroManual",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "JustificacionManual",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "MinutosRetraso",
                table: "Asistencias");

            migrationBuilder.DropColumn(
                name: "ModificadoUtc",
                table: "Asistencias");
        }
    }
}
