using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Gera.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CadastroOfertasRascunhos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [SPATIAL_IX_Ofertas_Localizacao] ON [dbo].[Ofertas];");

            migrationBuilder.AlterColumn<decimal>(
                name: "Valor",
                table: "Ofertas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "TipoPreco",
                table: "Ofertas",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Ofertas",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<Point>(
                name: "Localizacao",
                table: "Ofertas",
                type: "geography",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Ofertas",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                table: "Ofertas",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AtributosJson",
                table: "Ofertas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Ofertas",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Ofertas",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExcluidoEm",
                table: "Ofertas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadoEm",
                table: "Ofertas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "Ofertas",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.Sql("CREATE SPATIAL INDEX [SPATIAL_IX_Ofertas_Localizacao] ON [dbo].[Ofertas] ([Localizacao]) USING GEOGRAPHY_AUTO_GRID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [dbo].[Ofertas]
                    WHERE [CategoriaId] IS NULL OR [Localizacao] IS NULL
                        OR [Latitude] IS NULL OR [Longitude] IS NULL
                        OR [TipoPreco] IS NULL OR [Valor] IS NULL
                )
                    THROW 51000, 'Existem ofertas incompatíveis com o esquema anterior.', 1;
                """);
            migrationBuilder.Sql("DROP INDEX [SPATIAL_IX_Ofertas_Localizacao] ON [dbo].[Ofertas];");

            migrationBuilder.DropColumn(
                name: "AtributosJson",
                table: "Ofertas");

            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Ofertas");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Ofertas");

            migrationBuilder.DropColumn(
                name: "ExcluidoEm",
                table: "Ofertas");

            migrationBuilder.DropColumn(
                name: "PublicadoEm",
                table: "Ofertas");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Ofertas");

            migrationBuilder.AlterColumn<decimal>(
                name: "Valor",
                table: "Ofertas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TipoPreco",
                table: "Ofertas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Ofertas",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<Point>(
                name: "Localizacao",
                table: "Ofertas",
                type: "geography",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Ofertas",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                table: "Ofertas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            migrationBuilder.Sql("CREATE SPATIAL INDEX [SPATIAL_IX_Ofertas_Localizacao] ON [dbo].[Ofertas] ([Localizacao]) USING GEOGRAPHY_AUTO_GRID;");
        }
    }
}
