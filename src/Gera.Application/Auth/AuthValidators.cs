using FluentValidation;
using System.Text.RegularExpressions;

namespace Gera.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsuarioOuEmail).NotEmpty().WithMessage("Informe seu usuário ou e-mail.");
        RuleFor(x => x.Senha).NotEmpty().WithMessage("Informe sua senha.");
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe seu nome.").MaximumLength(150).WithMessage("O nome deve ter até 150 caracteres.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe seu e-mail.").EmailAddress().WithMessage("Informe um e-mail válido.");
        RuleFor(x => x.NomeUsuario).NotEmpty().WithMessage("Informe um nome de usuário.").MinimumLength(3).MaximumLength(30)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Use de 3 a 30 caracteres: letras, números, ponto, hífen ou sublinhado.");
        RuleFor(x => x.Telefone).NotEmpty().WithMessage("Informe seu telefone.")
            .Must(v => v is not null && Regex.IsMatch(v, "^[+()0-9 .-]+$") && Regex.Replace(v, "\\D", "").Length is >= 10 and <= 13)
            .WithMessage("Informe um telefone válido com DDD.");
        RuleFor(x => x.Senha).Must(IsValidPassword).WithMessage("A senha deve ter pelo menos 8 caracteres, uma letra e um número.");
        RuleFor(x => x.AceitouTermos).Equal(true).WithMessage("É necessário aceitar os Termos de Uso.");
        RuleFor(x => x.AceitouPrivacidade).Equal(true).WithMessage("É necessário aceitar a Política de Privacidade.");
    }

    internal static bool IsValidPassword(string? value) => !string.IsNullOrWhiteSpace(value)
        && value.Length >= 8 && value.Any(char.IsLetter) && value.Any(char.IsDigit);
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(x => x.Email).NotEmpty().WithMessage("Informe seu e-mail.").EmailAddress().WithMessage("Informe um e-mail válido.");
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Informe um e-mail válido.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token de redefinição inválido.");
        RuleFor(x => x.NovaSenha).Must(RegisterRequestValidator.IsValidPassword).WithMessage("A senha deve ter pelo menos 8 caracteres, uma letra e um número.");
    }
}
