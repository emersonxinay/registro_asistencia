using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace registroAsistencia.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocenteId",
                table: "Clases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CodigoDocente = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Departamento = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoAccesoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EsAdministrador = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clases_DocenteId",
                table: "Clases",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CodigoDocente",
                table: "Usuarios",
                column: "CodigoDocente",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            // Insertar usuario administrador por defecto
            migrationBuilder.Sql(@"
                INSERT INTO ""Usuarios"" (""Nombre"", ""Email"", ""CodigoDocente"", ""PasswordHash"", ""Departamento"", ""Activo"", ""CreadoUtc"", ""EsAdministrador"")
                VALUES ('Administrador', 'admin@quantumattend.edu', 'ADMIN001', '$2a$11$8EeVUoKLcBR8F7zCJvbKOOKrAy6vY.yKRbhfVBsHtmhUIqG1J.ZOe', 'Administración', true, NOW(), true);
            ");

            // Actualizar las clases existentes para referenciar al administrador
            migrationBuilder.Sql(@"
                UPDATE ""Clases"" 
                SET ""DocenteId"" = (SELECT ""Id"" FROM ""Usuarios"" WHERE ""CodigoDocente"" = 'ADMIN001' LIMIT 1)
                WHERE ""DocenteId"" IS NULL OR ""DocenteId"" = 0;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Clases_Usuarios_DocenteId",
                table: "Clases",
                column: "DocenteId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clases_Usuarios_DocenteId",
                table: "Clases");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Clases_DocenteId",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "DocenteId",
                table: "Clases");
        }
    }
}
