using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gera.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261124025500_MetadadosFotosOferta")]
public sealed class MetadadosFotosOferta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("EhCapa", "OfertaFotos", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("UrlMiniatura", "OfertaFotos", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.Sql("""
            ;WITH FotosOrdenadas AS (
                SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OfertaId] ORDER BY [Ordem], [Id]) AS [Posicao]
                FROM [dbo].[OfertaFotos]
            )
            UPDATE [foto]
            SET [EhCapa] = 1
            FROM [dbo].[OfertaFotos] AS [foto]
            INNER JOIN [FotosOrdenadas] AS [ordenada] ON [ordenada].[Id] = [foto].[Id]
            WHERE [ordenada].[Posicao] = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("EhCapa", "OfertaFotos");
        migrationBuilder.DropColumn("UrlMiniatura", "OfertaFotos");
    }
}
