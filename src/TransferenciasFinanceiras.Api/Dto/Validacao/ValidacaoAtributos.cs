using System.ComponentModel.DataAnnotations;
using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Api.Dto.Validacao;

/// <summary>Rejeita Guid.Empty — o que chega quando o campo é omitido no JSON ou enviado como "0000...".</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class GuidNaoVazioAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto) =>
        valor is Guid guid && guid == Guid.Empty
            ? new ValidationResult($"O campo {contexto.DisplayName} é obrigatório e não pode ser um Guid vazio.", [contexto.MemberName!])
            : ValidationResult.Success;
}

/// <summary>
/// Valor em reais: no máximo 2 casas decimais (as colunas são numeric(18,2) — sem isso,
/// R$ 0,001 passaria como "maior que zero" e seria gravado como R$ 0,00) e dentro do
/// que a coluna comporta. Nulo é aceito, para campos opcionais.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ValorMonetarioAttribute(bool permitirZero = false) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto)
    {
        if (valor is not decimal numero)
        {
            return ValidationResult.Success;
        }

        var erro = numero switch
        {
            <= 0 when !permitirZero => "deve ser maior que zero",
            < 0 => "não pode ser negativo",
            > ValoresMonetarios.Maximo => $"não pode passar de {ValoresMonetarios.Maximo:N2}",
            _ when !ValoresMonetarios.TemNoMaximoDuasCasas(numero) => "deve ter no máximo 2 casas decimais",
            _ => null
        };

        return erro is null
            ? ValidationResult.Success
            : new ValidationResult($"O campo {contexto.DisplayName} {erro}.", [contexto.MemberName!]);
    }
}
