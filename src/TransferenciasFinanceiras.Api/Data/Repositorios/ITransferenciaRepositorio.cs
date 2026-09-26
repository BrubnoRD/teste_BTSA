using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface ITransferenciaRepositorio
{
    Task<Transferencia?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lê a transferência bloqueando a linha (SELECT ... FOR UPDATE) até o fim da transação.
    /// Serializa cancelamento e execução de um mesmo agendamento: sem isso, os dois poderiam
    /// ler "Scheduled" ao mesmo tempo e o último a gravar venceria (ex.: dinheiro movido,
    /// mas status final "Cancelled").
    /// </summary>
    Task<Transferencia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default);

    void Adicionar(Transferencia transferencia);

    /// <summary>Quantas tentativas processadas (concluídas ou com falha) a conta de origem fez na última hora — regra 5.</summary>
    Task<int> ContarTentativasUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default);

    /// <summary>Soma dos valores já transferidos com sucesso pela conta de origem na última hora — regra 5.</summary>
    Task<decimal> SomarValorTransferidoUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default);

    /// <summary>Agendamentos com data/hora já vencida, prontos para execução pelo processador.</summary>
    Task<IReadOnlyList<Guid>> ObterIdsAgendamentosVencidosAsync(DateTime agora, CancellationToken ct = default);
}
