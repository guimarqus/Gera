using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gera.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarAutorOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutorId",
                table: "Orcamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [Orcamentos] SET [AutorId] = [PrestadorId]");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_AutorId",
                table: "Orcamentos",
                column: "AutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orcamentos_Usuarios_AutorId",
                table: "Orcamentos",
                column: "AutorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orcamentos_Usuarios_AutorId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_AutorId",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "AutorId",
                table: "Orcamentos");
        }
    }
}
