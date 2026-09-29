using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Service;

namespace TransferenciasFinanceiras.Api.HostedService;

public class ProcessadorTransferenciasAgendadas(IServiceScopeFactory fabricaEscopo, ILogger<ProcessadorTransferenciasAgendadas> log) : BackgroundService
{
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken tokenParada)
    {
        while (!tokenParada.IsCancellationRequested)
        {
            try
            {
                await ProcessarAgendamentosVencidosAsync(tokenParada);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Falha ao processar transferências agendadas.");
            }

            try
            {
                await Task.Delay(IntervaloVerificacao, tokenParada);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task ProcessarAgendamentosVencidosAsync(CancellationToken ct)
    {
        IReadOnlyList<Guid> idsVencidos;
        using (var escopo = fabricaEscopo.CreateScope())
        {
            var transferenciaRepositorio = escopo.ServiceProvider.GetRequiredService<ITransferenciaRepositorio>();
            var relogio = escopo.ServiceProvider.GetRequiredService<IProvedorDataHora>();
            idsVencidos = await transferenciaRepositorio.ObterIdsAgendamentosVencidosAsync(relogio.AgoraUtc, ct);
        }

        foreach (var idTransferencia in idsVencidos)
        {
            using var escopo = fabricaEscopo.CreateScope();
            var transferenciaServico = escopo.ServiceProvider.GetRequiredService<ITransferenciaServico>();

            try
            {
                await transferenciaServico.ExecutarAgendadaAsync(idTransferencia, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Falha ao executar a transferência agendada {IdTransferencia}.", idTransferencia);
            }
        }
    }
}
