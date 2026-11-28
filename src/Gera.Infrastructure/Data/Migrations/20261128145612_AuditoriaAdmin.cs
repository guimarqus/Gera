using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gera.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [dbo].[Papeis] WHERE [NormalizedName] = N'ADMINISTRADOR') INSERT INTO [dbo].[Papeis] ([Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'Administrador', N'ADMINISTRADOR', N'gera-administrador');");
            migrationBuilder.CreateTable(
                name: "AuditoriasAdmin",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdministradorId = table.Column<int>(type: "int", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Identificador = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriasAdmin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditoriasAdmin_Usuarios_AdministradorId",
                        column: x => x.AdministradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasAdmin_AdministradorId_CriadoEm",
                table: "AuditoriasAdmin",
                columns: new[] { "AdministradorId", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriasAdmin");
        }
    }
}
