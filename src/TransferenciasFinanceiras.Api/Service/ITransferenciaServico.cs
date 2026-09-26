using TransferenciasFinanceiras.Api.Dto.Transferencias;

namespace TransferenciasFinanceiras.Api.Service;

public interface ITransferenciaServico
{
    Task<TransferenciaResposta> ExecutarImediataAsync(CriarTransferenciaRequisicao requisicao, CancellationToken ct = default);

    Task<TransferenciaResposta> AgendarAsync(AgendarTransferenciaRequisicao requisicao, CancellationToken ct = default);

    Task<TransferenciaResposta> CancelarAsync(Guid idTransferencia, CancellationToken ct = default);

    Task<TransferenciaResposta> ObterAsync(Guid idTransferencia, CancellationToken ct = default);

    /// <summary>Chamado pelo processador de agendamentos (ver HostedService/ProcessadorTransferenciasAgendadas) quando o horário do agendamento chega.</summary>
    Task ExecutarAgendadaAsync(Guid idTransferencia, CancellationToken ct = default);
}
