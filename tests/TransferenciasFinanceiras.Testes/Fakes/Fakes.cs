using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Service;

namespace TransferenciasFinanceiras.Testes.Fakes;

public sealed class RelogioFixo(DateTime agoraUtc) : IProvedorDataHora
{
    public DateTime AgoraUtc { get; private set; } = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);

    public void Avancar(TimeSpan intervalo) => AgoraUtc += intervalo;
}

public sealed class ContaRepositorioEmMemoria : IContaRepositorio
{
    private readonly Dictionary<Guid, Conta> _contas = [];

    public Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_contas.GetValueOrDefault(id));

    public Task<Conta?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default) => ObterPorIdAsync(id, ct);

    public void Adicionar(Conta conta) => _contas[conta.Id] = conta;
}

public sealed class TransferenciaRepositorioEmMemoria : ITransferenciaRepositorio
{
    private readonly List<Transferencia> _transferencias = [];

    public IReadOnlyList<Transferencia> Todas => _transferencias;

    public Task<Transferencia?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_transferencias.SingleOrDefault(t => t.Id == id));

    public Task<Transferencia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default) => ObterPorIdAsync(id, ct);

    public void Adicionar(Transferencia transferencia) => _transferencias.Add(transferencia);

    public Task<int> ContarTentativasUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default) =>
        Task.FromResult(ProcessadasNaUltimaHora(idContaOrigem, agora)
            .Count(t => t.Status is StatusTransferencia.Completed or StatusTransferencia.Failed));

    public Task<decimal> SomarValorTransferidoUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default) =>
        Task.FromResult(ProcessadasNaUltimaHora(idContaOrigem, agora)
            .Where(t => t.Status == StatusTransferencia.Completed)
            .Sum(t => t.Valor));

    public Task<IReadOnlyList<Guid>> ObterIdsAgendamentosVencidosAsync(DateTime agora, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(_transferencias
            .Where(t => t.Status == StatusTransferencia.Scheduled && t.AgendadaPara <= agora)
            .OrderBy(t => t.AgendadaPara)
            .Select(t => t.Id)
            .ToList());

    private IEnumerable<Transferencia> ProcessadasNaUltimaHora(Guid idContaOrigem, DateTime agora) =>
        _transferencias.Where(t => t.IdContaOrigem == idContaOrigem
                                   && t.ProcessadaEm > agora.AddHours(-1)
                                   && t.ProcessadaEm <= agora);
}

public sealed class UnidadeDeTrabalhoEmMemoria : IUnidadeDeTrabalho
{
    public Task SalvarAlteracoesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> acao, CancellationToken ct = default) => acao(ct);
}
