using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Gera.Domain.Exceptions;

namespace Gera.Application.Ofertas;

public static partial class TagsOferta
{
    private static readonly StringComparer Comparador = StringComparer.Create(
        CultureInfo.GetCultureInfo("pt-BR"), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

    public static string[] Normalizar(IReadOnlyList<string> tags)
    {
        if (tags.Count > 5)
            throw new DomainException("Informe no máximo cinco tags.");

        var resultado = new HashSet<string>(Comparador);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Informe tags com 1 a 50 caracteres.");
            var normalizada = Espacos().Replace(tag.Trim(), " ").Normalize(NormalizationForm.FormC);
            if (normalizada.Length > 50)
                throw new DomainException("Informe tags com 1 a 50 caracteres.");
            resultado.Add(normalizada);
        }

        return resultado.ToArray();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();
}
