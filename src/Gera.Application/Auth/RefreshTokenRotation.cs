using Gera.Domain.Entities;

namespace Gera.Application.Auth;

public static class RefreshTokenRotation
{
    public static void Rotate(RefreshToken current, string replacementHash, DateTime now)
    {
        current.RevogadoEm = now;
        current.SubstituidoPorHash = replacementHash;
    }

    public static void RevokeActiveChain(IEnumerable<RefreshToken> tokens, DateTime now)
    {
        foreach (var token in tokens.Where(token => token.RevogadoEm is null))
            token.RevogadoEm = now;
    }
}
