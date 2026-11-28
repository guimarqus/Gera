using System.Security.Claims;
using Gera.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Gera.Api.Security;

public sealed class AdministradorRequirement : IAuthorizationRequirement;

public sealed class AdministradorAuthorizationHandler(UserManager<Usuario> usuarios) : AuthorizationHandler<AdministradorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministradorRequirement requirement)
    {
        var id = context.User.FindFirstValue("sub");
        if (!int.TryParse(id, out var usuarioId)) return;
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is not null && usuario.Ativo && await usuarios.IsInRoleAsync(usuario, "Administrador"))
            context.Succeed(requirement);
    }
}
