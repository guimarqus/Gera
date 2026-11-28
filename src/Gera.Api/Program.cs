using Gera.Api.Middlewares;
using Gera.Infrastructure;
using Microsoft.OpenApi.Models;
using Serilog;
using Gera.Application.Auth;
using Gera.Infrastructure.Services;
using Gera.Application.Catalog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Gera.Api.Security;
using Gera.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Serviços de Infraestrutura (AppDbContext, Identity, SQL Server + NetTopologySuite)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoriasService, CategoriasService>();
builder.Services.AddScoped<Gera.Application.Ofertas.OfertaService>();
builder.Services.AddScoped<Gera.Application.Ofertas.OfertaFeedService>();
builder.Services.AddScoped<Gera.Application.Ofertas.ModeloOfertaService>();
builder.Services.AddScoped<Gera.Application.Ofertas.OfertaFotosService>();
builder.Services.AddOptions<Gera.Application.Ofertas.OfertaOptions>()
    .BindConfiguration("Ofertas")
    .Validate(options => options.LimiteAtivasPorUsuario > 0, "O limite de ofertas ativas deve ser positivo.")
    .ValidateOnStart();
builder.Services.AddScoped<IProfileSummaryService, ProfileSummaryService>();
builder.Services.AddScoped<PrestadorPublicoService>();
builder.Services.AddScoped<Gera.Application.Solicitacoes.SolicitacaoService>();
builder.Services.AddScoped<Gera.Application.Mensagens.MensagemService>();
builder.Services.AddScoped<Gera.Application.Mensagens.NotificacaoService>();
builder.Services.AddScoped<Gera.Application.Confianca.ConfiancaService>();
builder.Services.AddScoped<Gera.Application.Admin.AdminService>();
builder.Services.AddScoped<IAuthorizationHandler, AdministradorAuthorizationHandler>();
var smtpConfigurado = !string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]);
if (!builder.Environment.IsDevelopment() || smtpConfigurado)
    builder.Services.AddOptions<SmtpEmailOptions>().BindConfiguration("Email:Smtp")
        .Validate(options => options.IsValid(), "Configure Email:Smtp com host, porta, TLS, remetente e credenciais SMTP válidas.")
        .ValidateOnStart();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailSender>(provider => !builder.Environment.IsDevelopment() || smtpConfigurado
    ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider)
    : ActivatorUtilities.CreateInstance<DevelopmentEmailSender>(provider));

// Controladores e FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var jwtSecret = builder.Configuration["JwtSettings:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
        throw new InvalidOperationException("Configure JwtSettings:Secret com ao menos 32 bytes via User Secrets.");
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "GeraApi",
        ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "GeraWeb",
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero, NameClaimType = "name"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var subject = context.Principal?.FindFirst("sub")?.Value;
            if (!int.TryParse(subject, out var userId)) { context.Fail("Identidade do usuário inválida."); return; }
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null || !user.Ativo) context.Fail("Conta indisponível.");
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Administrador", policy => policy.Requirements.Add(new AdministradorRequirement()));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("fotos-ofertas", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("feed-ofertas", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("midia-chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("chat-mensagens", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Configuração do Swagger com suporte a Bearer JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gera API",
        Version = "v1",
        Description = "API RESTful do Gera - Marketplace de Serviços Gerais"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação JWT usando o cabeçalho Authorization. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuração de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware Global de Tratamento de Exceções (ProblemDetails)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gera API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();

app.UseCors("FrontendPolicy");

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Gera.Api.Hubs.ChatHub>("/hubs/chat").RequireAuthorization();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    app = "Gera API"
})).WithName("HealthCheck").AllowAnonymous();

app.Run();

// Necessário para testes de integração com WebApplicationFactory<Program>
public partial class Program { }
