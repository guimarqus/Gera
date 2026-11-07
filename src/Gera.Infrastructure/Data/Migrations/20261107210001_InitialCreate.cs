using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gera.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Icone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CategoriaPaiId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_Categorias_CategoriaPaiId",
                        column: x => x.CategoriaPaiId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Papeis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Papeis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Biografia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Verificado = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    RaioAtendimentoPadraoKm = table.Column<int>(type: "int", nullable: false),
                    NotificacoesAtivas = table.Column<bool>(type: "bit", nullable: false),
                    PapelAtivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Cliente"),
                    RefreshToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RefreshTokenExpiracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PapelClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PapelClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PapelClaims_Papeis_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Papeis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bloqueios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BloqueadorId = table.Column<int>(type: "int", nullable: false),
                    BloqueadoId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloqueios_Usuarios_BloqueadoId",
                        column: x => x.BloqueadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bloqueios_Usuarios_BloqueadorId",
                        column: x => x.BloqueadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenuncianteId = table.Column<int>(type: "int", nullable: false),
                    TipoAlvo = table.Column<int>(type: "int", nullable: false),
                    AlvoId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParecerModerador = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModeradorId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ResolvidoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Denuncias_Usuarios_DenuncianteId",
                        column: x => x.DenuncianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ReferenciaId = table.Column<int>(type: "int", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ofertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrestadorId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Localizacao = table.Column<Point>(type: "geography", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RaioKm = table.Column<int>(type: "int", nullable: false),
                    AreaAtendida = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TipoPreco = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorNegociavel = table.Column<bool>(type: "bit", nullable: false),
                    Disponibilidade = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FormasPagamento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NotaMedia = table.Column<double>(type: "float", nullable: false),
                    TotalAvaliacoes = table.Column<int>(type: "int", nullable: false),
                    TotalServicos = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ofertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ofertas_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ofertas_Usuarios_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioClaims_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UsuarioLogins_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPapeis",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPapeis", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UsuarioPapeis_Papeis_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Papeis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPapeis_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UsuarioTokens_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OfertaId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favoritos_Ofertas_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "Ofertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favoritos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfertaFotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfertaId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertaFotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfertaFotos_Ofertas_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "Ofertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfertaTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfertaId = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertaTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfertaTags_Ofertas_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "Ofertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    PrestadorId = table.Column<int>(type: "int", nullable: false),
                    OfertaId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Etapa = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DataAgendada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AceiteCliente = table.Column<bool>(type: "bit", nullable: false),
                    AceitePrestador = table.Column<bool>(type: "bit", nullable: false),
                    ConcluidoPorCliente = table.Column<bool>(type: "bit", nullable: false),
                    ConcluidoPorPrestador = table.Column<bool>(type: "bit", nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanceladoPorId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Ofertas_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "Ofertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Usuarios_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Avaliacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    AvaliadorId = table.Column<int>(type: "int", nullable: false),
                    AvaliadoId = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<int>(type: "int", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resposta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RespondidoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avaliacoes", x => x.Id);
                    table.CheckConstraint("CK_Avaliacoes_Nota", "[Nota] >= 1 AND [Nota] <= 5");
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Usuarios_AvaliadoId",
                        column: x => x.AvaliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Usuarios_AvaliadorId",
                        column: x => x.AvaliadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    PrestadorId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrazoEmDias = table.Column<int>(type: "int", nullable: true),
                    DataPrevisao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OQueEstaIncluso = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RespondidoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orcamentos_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orcamentos_Usuarios_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    PagadorId = table.Column<int>(type: "int", nullable: false),
                    RecebedorId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Metodo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IdentificadorTransacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    PagoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Usuarios_PagadorId",
                        column: x => x.PagadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Usuarios_RecebedorId",
                        column: x => x.RecebedorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoHistoricos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    EtapaAnterior = table.Column<int>(type: "int", nullable: false),
                    EtapaNova = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OcorreuEm = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoHistoricos_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitacaoHistoricos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mensagens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    RemetenteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MidiaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrcamentoId = table.Column<int>(type: "int", nullable: true),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensagens_Orcamentos_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Mensagens_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mensagens_Usuarios_RemetenteId",
                        column: x => x.RemetenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Ativo", "CategoriaPaiId", "Descricao", "Icone", "Nome", "Ordem" },
                values: new object[,]
                {
                    { 1, true, null, "Obras, reformas, alvenaria, pisos e acabamentos", "Hammer", "Construção", 1 },
                    { 2, true, null, "Instalações elétricas, fiação, quadros e iluminação", "Zap", "Elétrica", 2 },
                    { 3, true, null, "Encanamentos, vazamentos, pias, torneiras e caixas d'água", "Droplet", "Hidráulica", 3 },
                    { 4, true, null, "Pintura residencial, predial, texturas e vernizes", "Paintbrush", "Pintura", 4 },
                    { 5, true, null, "Faxinas, pós-obra, estofados e higienização geral", "Sparkles", "Limpeza", 5 },
                    { 6, true, null, "Transporte de cargas, mudanças e carretos", "Truck", "Fretes e Mudanças", 6 },
                    { 7, true, null, "Poda, corte de grama, paisagismo e limpeza de terreno", "Trees", "Jardinagem", 7 },
                    { 8, true, null, "Marido de aluguel, montagem de móveis e pequenos consertos", "Wrench", "Reparos Domésticos", 8 },
                    { 101, true, 1, "Paredes, muros, vigas e fundações", "BrickWall", "Alvenaria", 1 },
                    { 102, true, 1, "Gesso, molduras, rodapés e detalhes", "Layers", "Acabamento", 2 },
                    { 103, true, 1, "Porcelanato, cerâmica, azulejos e pastilhas", "Grid", "Piso e Revestimento", 3 },
                    { 104, true, 1, "Construção e conserto de telhados e rufos", "Home", "Telhado e Calhas", 4 },
                    { 105, true, 1, "Chapisco, emboço, reboco e concretagem", "HardHat", "Reboco e Laje", 5 },
                    { 106, true, 1, "Sapatas, colunas, brocas e baldrames", "ShieldCheck", "Estrutura e Fundação", 6 },
                    { 201, true, 2, "Tomadas, interruptores e chuveiros", "Plug", "Instalações Residenciais", 1 },
                    { 202, true, 2, "Substituição completa ou parcial de condutores", "Cable", "Troca de Fiação", 2 },
                    { 203, true, 2, "Montagem, dimensionamento e substituição", "Cpu", "Quadros de Luz e Disjuntores", 3 },
                    { 204, true, 2, "Instalação e adequação de padrão de energia", "ShieldAlert", "Padrão de Entrada", 4 },
                    { 205, true, 2, "Fitas LED, pendentes, spots e lustres", "Sun", "Iluminação e Lustres", 5 },
                    { 206, true, 2, "Diagnóstico de curto-circuito e panes", "Activity", "Manutenção e Reparos Elétricos", 6 },
                    { 301, true, 3, "Pias, ralos, vasos sanitários e caixas de gordura", "Disc", "Desentupimento", 1 },
                    { 302, true, 3, "Tubulações de água fria, quente e esgoto", "GitCommit", "Encanamento Geral", 2 },
                    { 303, true, 3, "Detecção não destrutiva de infiltrações", "Search", "Caça-Vazamento", 3 },
                    { 304, true, 3, "Torneiras, registros, cubas e vasos", "CircleDot", "Instalação de Louças e Metais", 4 },
                    { 305, true, 3, "Limpeza, instalação e pressurizadores", "Archive", "Caixa d'Água e Bombas", 5 },
                    { 306, true, 3, "Aquecedores a gás, elétricos e solares", "Flame", "Aquecedores", 6 },
                    { 401, true, 4, "Tetos, paredes, portas e janelas", "Paintbrush", "Pintura Residencial Interna", 1 },
                    { 402, true, 4, "Proteção e acabamento externo resistente", "Building", "Pintura Externa e Fachadas", 2 },
                    { 403, true, 4, "Cimento queimado, marmorato e grafiato", "Feather", "Texturas e Efeitos Decorativos", 3 },
                    { 404, true, 4, "Decks, pergolados, portas e móveis", "Box", "Envernizamento de Madeiras", 4 },
                    { 405, true, 4, "Proteção antiferrugem e esmalte sintético", "Shield", "Pintura de Portões e Grades", 5 },
                    { 501, true, 5, "Limpeza geral e manutenção do lar", "CheckCircle", "Faxina Diária / Residencial", 1 },
                    { 502, true, 5, "Remoção de resíduos pesados e tintas", "Trash2", "Limpeza Pós-Obra", 2 },
                    { 503, true, 5, "Sofás, colchões, tapetes e cortinas", "Armchair", "Limpeza de Estofados", 3 },
                    { 504, true, 5, "Lojas, consultórios e escritórios", "Briefcase", "Limpeza Comercial", 4 },
                    { 505, true, 5, "Fachadas envidraçadas e sacadas", "Maximize2", "Limpeza de Vidros e Janelas", 5 },
                    { 601, true, 6, "Carretos e transporte de pequenos volumes", "Package", "Pequenos Fretes", 1 },
                    { 602, true, 6, "Embalagem, transporte e descarregamento", "Truck", "Mudança Residencial Completa", 2 },
                    { 603, true, 6, "Geladeiras, fogões, sofás e camas", "Tv", "Transporte de Móveis e Eletros", 3 },
                    { 604, true, 6, "Entregas rápidas no mesmo dia", "Clock", "Coletas e Entregas Expressas", 4 },
                    { 605, true, 6, "Subida de peças grandes por sacadas", "ArrowUpCircle", "Içamento de Cargas", 5 },
                    { 701, true, 7, "Aparação de gramados e áreas verdes", "Scissors", "Corte de Grama e Roçagem", 1 },
                    { 702, true, 7, "Poda ornamental e de segurança", "Crop", "Poda de Árvores e Cercas Vivas", 2 },
                    { 703, true, 7, "Criação de canteiros, adubação e mudas", "Heart", "Paisagismo e Plantio", 3 },
                    { 704, true, 7, "Retirada de mato alto e entulho vegetal", "Slash", "Limpeza de Terrenos", 4 },
                    { 705, true, 7, "Contratos semanais ou mensais", "Calendar", "Manutenção Periódica de Jardim", 5 },
                    { 801, true, 8, "Serviços gerais e reparos rápidos para o lar", "Wrench", "Marido de Aluguel", 1 },
                    { 802, true, 8, "Montagem e desmontagem de armários, mesas e racks", "Tool", "Montagem de Móveis", 2 },
                    { 803, true, 8, "Varões, persianas, espelhos e suportes de TV", "Layout", "Instalação de Cortinas e Quadros", 3 },
                    { 804, true, 8, "Troca de miolos, trincos, ajustes e dobradiças", "Key", "Fechaduras e Portas", 4 },
                    { 805, true, 8, "Silicone em box, troca de maçanetas e vedação", "CheckSquare", "Pequenos Reparos em Geral", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadoId",
                table: "Avaliacoes",
                column: "AvaliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes",
                column: "AvaliadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_SolicitacaoId_AvaliadorId",
                table: "Avaliacoes",
                columns: new[] { "SolicitacaoId", "AvaliadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueios_BloqueadoId",
                table: "Bloqueios",
                column: "BloqueadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueios_BloqueadorId_BloqueadoId",
                table: "Bloqueios",
                columns: new[] { "BloqueadorId", "BloqueadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_CategoriaPaiId",
                table: "Categorias",
                column: "CategoriaPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_DenuncianteId",
                table: "Denuncias",
                column: "DenuncianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Status_CriadoEm",
                table: "Denuncias",
                columns: new[] { "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_OfertaId",
                table: "Favoritos",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_UsuarioId_OfertaId",
                table: "Favoritos",
                columns: new[] { "UsuarioId", "OfertaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_OrcamentoId",
                table: "Mensagens",
                column: "OrcamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_RemetenteId",
                table: "Mensagens",
                column: "RemetenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_SolicitacaoId_DataHora",
                table: "Mensagens",
                columns: new[] { "SolicitacaoId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioId_Lida",
                table: "Notificacoes",
                columns: new[] { "UsuarioId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_OfertaFotos_OfertaId",
                table: "OfertaFotos",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ofertas_CategoriaId_Status",
                table: "Ofertas",
                columns: new[] { "CategoriaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Ofertas_PrestadorId",
                table: "Ofertas",
                column: "PrestadorId");

            migrationBuilder.CreateIndex(
                name: "SPATIAL_IX_Ofertas_Localizacao",
                table: "Ofertas",
                column: "Localizacao");

            migrationBuilder.CreateIndex(
                name: "IX_OfertaTags_OfertaId",
                table: "OfertaTags",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_OfertaTags_Tag",
                table: "OfertaTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_PrestadorId",
                table: "Orcamentos",
                column: "PrestadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_SolicitacaoId_Status",
                table: "Orcamentos",
                columns: new[] { "SolicitacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PagadorId",
                table: "Pagamentos",
                column: "PagadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_RecebedorId",
                table: "Pagamentos",
                column: "RecebedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_SolicitacaoId_Status",
                table: "Pagamentos",
                columns: new[] { "SolicitacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Papeis",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PapelClaims_RoleId",
                table: "PapelClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoHistoricos_SolicitacaoId_OcorreuEm",
                table: "SolicitacaoHistoricos",
                columns: new[] { "SolicitacaoId", "OcorreuEm" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoHistoricos_UsuarioId",
                table: "SolicitacaoHistoricos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_ClienteId_Etapa",
                table: "Solicitacoes",
                columns: new[] { "ClienteId", "Etapa" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_OfertaId",
                table: "Solicitacoes",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_PrestadorId_Etapa",
                table: "Solicitacoes",
                columns: new[] { "PrestadorId", "Etapa" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClaims_UserId",
                table: "UsuarioClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioLogins_UserId",
                table: "UsuarioLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPapeis_RoleId",
                table: "UsuarioPapeis",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Usuarios",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Usuarios",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avaliacoes");

            migrationBuilder.DropTable(
                name: "Bloqueios");

            migrationBuilder.DropTable(
                name: "Denuncias");

            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.DropTable(
                name: "Mensagens");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "OfertaFotos");

            migrationBuilder.DropTable(
                name: "OfertaTags");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "PapelClaims");

            migrationBuilder.DropTable(
                name: "SolicitacaoHistoricos");

            migrationBuilder.DropTable(
                name: "UsuarioClaims");

            migrationBuilder.DropTable(
                name: "UsuarioLogins");

            migrationBuilder.DropTable(
                name: "UsuarioPapeis");

            migrationBuilder.DropTable(
                name: "UsuarioTokens");

            migrationBuilder.DropTable(
                name: "Orcamentos");

            migrationBuilder.DropTable(
                name: "Papeis");

            migrationBuilder.DropTable(
                name: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "Ofertas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
