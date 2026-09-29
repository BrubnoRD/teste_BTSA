using TransferenciasFinanceiras.Api.Dto.Transferencias;

namespace TransferenciasFinanceiras.Api.Service;

public interface ITransferenciaServico
{
    Task<TransferenciaResposta> ExecutarImediataAsync(CriarTransferenciaRequisicao requisicao, CancellationToken ct = default);

    Task<TransferenciaResposta> AgendarAsync(AgendarTransferenciaRequisicao requisicao, CancellationToken ct = default);

    Task<TransferenciaResposta> CancelarAsync(Guid idTransferencia, CancellationToken ct = default);

    Task<TransferenciaResposta> ObterAsync(Guid idTransferencia, CancellationToken ct = default);

    Task ExecutarAgendadaAsync(Guid idTransferencia, CancellationToken ct = default);
}
