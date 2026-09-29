using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface ITransferenciaRepositorio
{
    Task<Transferencia?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<Transferencia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default);

    void Adicionar(Transferencia transferencia);

    Task<int> ContarTentativasUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default);

    Task<decimal> SomarValorTransferidoUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ObterIdsAgendamentosVencidosAsync(DateTime agora, CancellationToken ct = default);
}
