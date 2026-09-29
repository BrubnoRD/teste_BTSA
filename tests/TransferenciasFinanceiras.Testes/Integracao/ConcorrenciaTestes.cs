using Microsoft.EntityFrameworkCore;
using TransferenciasFinanceiras.Api.Data;
using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Dto.Transferencias;
using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Testes.Integracao;

[Trait("Categoria", "Integracao")]
public class ConcorrenciaTestes(PostgresFixture banco) : IClassFixture<PostgresFixture>
{
    private async Task<Conta> CriarContaAsync(decimal saldo)
    {
        var conta = Conta.Abrir("Titular", saldo, limiteChequeEspecial: 0m, DateTime.UtcNow,
            limiteTransferenciaDiurno: 1_000_000m, maxTentativasPorHoraDiurno: 100,
            limiteTransferenciaNoturno: 1_000_000m, maxTentativasPorHoraNoturno: 100);

        await using var db = banco.CriarContexto();
        db.Contas.Add(conta);
        await db.SaveChangesAsync();
        return conta;
    }

    private async Task<Conta> RecarregarAsync(Guid id)
    {
        await using var db = banco.CriarContexto();
        return await db.Contas.AsNoTracking().SingleAsync(c => c.Id == id);
    }

    [Fact]
    public async Task TransferenciasSimultaneasDoMesmoSaldo_SoAsQueCabemNoSaldoSaoConcluidas()
    {
        var origem = await CriarContaAsync(saldo: 100m);
        var destino = await CriarContaAsync(saldo: 0m);
        var relogio = PostgresFixture.RelogioMeioDiaBrasilia();
        using var largada = new SemaphoreSlim(0);

        var tarefas = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            var (servico, db) = banco.CriarServico(relogio);
            await using (db)
            {
                await largada.WaitAsync();
                return await servico.ExecutarImediataAsync(new CriarTransferenciaRequisicao(origem.Id, destino.Id, 30m));
            }
        })).ToList();

        largada.Release(tarefas.Count);
        var resultados = await Task.WhenAll(tarefas);

        Assert.Equal(3, resultados.Count(r => r.Status == StatusTransferencia.Completed));
        Assert.Equal(7, resultados.Count(r => r.Status == StatusTransferencia.Failed));
        Assert.Equal(10m, (await RecarregarAsync(origem.Id)).Saldo);
        Assert.Equal(90m, (await RecarregarAsync(destino.Id)).Saldo);
    }

    [Fact]
    public async Task TransferenciasCruzadasSimultaneas_NaoCausamImpasse()
    {
        var contaA = await CriarContaAsync(saldo: 1_000m);
        var contaB = await CriarContaAsync(saldo: 1_000m);
        var relogio = PostgresFixture.RelogioMeioDiaBrasilia();

        var tarefas = Enumerable.Range(0, 20).Select(i => Task.Run(async () =>
        {
            var (servico, db) = banco.CriarServico(relogio);
            await using (db)
            {
                var (de, para) = i % 2 == 0 ? (contaA, contaB) : (contaB, contaA);
                return await servico.ExecutarImediataAsync(new CriarTransferenciaRequisicao(de.Id, para.Id, 10m));
            }
        }));

        var resultados = await Task.WhenAll(tarefas);

        Assert.All(resultados, r => Assert.Equal(StatusTransferencia.Completed, r.Status));
        Assert.Equal(2_000m, (await RecarregarAsync(contaA.Id)).Saldo + (await RecarregarAsync(contaB.Id)).Saldo);
    }

    [Fact]
    public async Task CancelamentoDuranteAExecucaoDoAgendamento_EsperaENaoSobrescreveOResultado()
    {
        var origem = await CriarContaAsync(saldo: 100m);
        var destino = await CriarContaAsync(saldo: 0m);
        var relogio = PostgresFixture.RelogioMeioDiaBrasilia();

        Guid idAgendada;
        var (servicoAgendamento, dbAgendamento) = banco.CriarServico(relogio);
        await using (dbAgendamento)
        {
            var agendada = await servicoAgendamento.AgendarAsync(
                new AgendarTransferenciaRequisicao(origem.Id, destino.Id, 50m, relogio.AgoraUtc.AddMinutes(1)));
            idAgendada = agendada.Id;
        }

        await using var dbProcessador = banco.CriarContexto();
        await using var transacaoProcessador = await dbProcessador.Database.BeginTransactionAsync();
        var emExecucao = await new TransferenciaRepositorio(dbProcessador).ObterParaAtualizacaoAsync(idAgendada);

        var (servicoCancelamento, dbCancelamento) = banco.CriarServico(relogio);
        await using (dbCancelamento)
        {
            var cancelamento = servicoCancelamento.CancelarAsync(idAgendada);

            await Task.Delay(500);
            Assert.False(cancelamento.IsCompleted, "O cancelamento deveria esperar o bloqueio da execução.");

            emExecucao!.MarcarComoProcessando();
            emExecucao.MarcarComoConcluida(relogio.AgoraUtc);
            await dbProcessador.SaveChangesAsync();
            await transacaoProcessador.CommitAsync();

            await Assert.ThrowsAsync<TransferenciaNaoCancelavelExcecao>(() => cancelamento);
        }

        await using var dbVerificacao = banco.CriarContexto();
        var final = await dbVerificacao.Transferencias.AsNoTracking().SingleAsync(t => t.Id == idAgendada);
        Assert.Equal(StatusTransferencia.Completed, final.Status);
    }
}
