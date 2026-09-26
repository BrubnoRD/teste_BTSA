using System.ComponentModel.DataAnnotations;
using TransferenciasFinanceiras.Api.Dto.Validacao;

namespace TransferenciasFinanceiras.Api.Dto.Contas;

public sealed record CriarContaRequisicao(
    [Required(ErrorMessage = "O campo NomeTitular é obrigatório.")]
    [StringLength(200, ErrorMessage = "O campo NomeTitular deve ter no máximo 200 caracteres.")]
    string NomeTitular,
    [ValorMonetario(permitirZero: true)] decimal SaldoInicial,
    [ValorMonetario(permitirZero: true)] decimal LimiteChequeEspecial,
    [ValorMonetario] decimal? LimiteTransferenciaDiurno = null,
    [Range(1, 1_000, ErrorMessage = "O campo MaxTentativasPorHoraDiurno deve estar entre 1 e 1000.")] int? MaxTentativasPorHoraDiurno = null,
    [ValorMonetario] decimal? LimiteTransferenciaNoturno = null,
    [Range(1, 1_000, ErrorMessage = "O campo MaxTentativasPorHoraNoturno deve estar entre 1 e 1000.")] int? MaxTentativasPorHoraNoturno = null);
