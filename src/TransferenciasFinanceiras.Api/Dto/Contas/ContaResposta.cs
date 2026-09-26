using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Api.Dto.Contas;

public sealed record ContaResposta(
    Guid Id,
    string NomeTitular,
    decimal Saldo,
    decimal LimiteChequeEspecial,
    decimal SaldoDisponivel,
    StatusConta Status,
    decimal LimiteTransferenciaDiurno,
    int MaxTentativasPorHoraDiurno,
    decimal LimiteTransferenciaNoturno,
    int MaxTentativasPorHoraNoturno)
{
    public static ContaResposta DoDominio(Conta conta) => new(
        conta.Id,
        conta.NomeTitular,
        conta.Saldo,
        conta.LimiteChequeEspecial,
        conta.SaldoDisponivel,
        conta.Status,
        conta.LimiteTransferenciaDiurno,
        conta.MaxTentativasPorHoraDiurno,
        conta.LimiteTransferenciaNoturno,
        conta.MaxTentativasPorHoraNoturno);
}
