using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gera.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261124022700_CadastrarPapelPrestador")]
public sealed class CadastrarPapelPrestador : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Papeis] WHERE [NormalizedName] = N'PRESTADOR')
                INSERT INTO [dbo].[Papeis] ([Name], [NormalizedName], [ConcurrencyStamp])
                VALUES (N'Prestador', N'PRESTADOR', N'gera-prestador');
            """);

    // Preserva o papel e as atribuições de bancos que já o utilizam.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
