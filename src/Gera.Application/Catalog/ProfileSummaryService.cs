using Gera.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Gera.Application.Catalog;

public sealed class ProfileSummaryService(UserManager<Usuario> userManager) : IProfileSummaryService
{
    public async Task<ProfileSummaryDto?> GetAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null) return null;
        var roles = await userManager.GetRolesAsync(usuario);
        return new ProfileSummaryDto(usuario.Nome, usuario.FotoUrl, roles.ToArray(), usuario.PapelAtivo);
    }
}
