using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Service;

namespace TransferenciasFinanceiras.Api.HostedService;

/// <summary>
/// Processa agendamentos vencidos (regra 6). É deliberadamente simples — apenas uma
/// verificação periódica dentro do próprio processo da API — porque mensageria/processador
/// dedicado foi marcado como diferencial fora do escopo desta entrega; ainda assim
/// algum mecanismo de execução é necessário para a funcionalidade de agendamento
/// existir de fato. Ver README para as limitações dessa abordagem (não escala para
/// múltiplas réplicas da API sem um bloqueio distribuído).
/// </summary>
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
                // encerramento normal da aplicação
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
            // Um escopo (e portanto um DbContext) por agendamento: se o mesmo contexto fosse
            // reaproveitado, o EF devolveria contas/transferências já rastreadas com valores
            // de antes da última leitura, em vez do que está no banco agora.
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
