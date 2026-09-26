using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Api.Dto.Transferencias;

public sealed record TransferenciaResposta(
    Guid Id,
    Guid IdContaOrigem,
    Guid IdContaDestino,
    decimal Valor,
    StatusTransferencia Status,
    DateTime? AgendadaPara,
    DateTime CriadaEm,
    DateTime? ProcessadaEm,
    string? MotivoFalha)
{
    public static TransferenciaResposta DoDominio(Transferencia transferencia) => new(
        transferencia.Id,
        transferencia.IdContaOrigem,
        transferencia.IdContaDestino,
        transferencia.Valor,
        transferencia.Status,
        transferencia.AgendadaPara,
        transferencia.CriadaEm,
        transferencia.ProcessadaEm,
        transferencia.MotivoFalha);
}
