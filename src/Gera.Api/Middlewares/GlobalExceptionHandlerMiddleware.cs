using System.Net;
using System.Text.Json;
using Gera.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Erro não tratado capturado pelo middleware: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, "Recurso não encontrado", notFound.Message),
            NaoAutorizadoException unauthorized => (HttpStatusCode.Forbidden, "Acesso não autorizado", unauthorized.Message),
            TransicaoInvalidaException transicao => (HttpStatusCode.BadRequest, "Transição de etapa inválida", transicao.Message),
            DomainException domain => (HttpStatusCode.BadRequest, "Regra de negócio violada", domain.Message),
            _ => (HttpStatusCode.InternalServerError, "Erro interno do servidor", "Ocorreu um erro inesperado ao processar sua solicitação.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

