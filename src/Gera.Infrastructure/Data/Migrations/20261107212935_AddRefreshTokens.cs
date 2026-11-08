using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gera.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceitouPrivacidade",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceitouTermos",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacidadeAceitaEm",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermosAceitosEm",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.DropColumn(name: "RefreshToken", table: "Usuarios");
            migrationBuilder.DropColumn(name: "RefreshTokenExpiracao", table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevogadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubstituidoPorHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IpCriacao = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId_ExpiraEm",
                table: "RefreshTokens",
                columns: new[] { "UsuarioId", "ExpiraEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AceitouPrivacidade",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AceitouTermos",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PrivacidadeAceitaEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TermosAceitosEm",
                table: "Usuarios");

            migrationBuilder.AddColumn<string>(name: "RefreshToken", table: "Usuarios", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "RefreshTokenExpiracao", table: "Usuarios", type: "datetime2", nullable: true);
        }
    }
}
