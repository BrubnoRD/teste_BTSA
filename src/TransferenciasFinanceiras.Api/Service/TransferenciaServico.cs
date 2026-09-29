using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Dto.Transferencias;
using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Api.Service;

public sealed class TransferenciaServico(
    IContaRepositorio contas,
    ITransferenciaRepositorio transferencias,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IProvedorDataHora relogio) : ITransferenciaServico
{
    public async Task<TransferenciaResposta> ExecutarImediataAsync(CriarTransferenciaRequisicao requisicao, CancellationToken ct = default)
    {
        var agora = relogio.AgoraUtc;

        var transferencia = Transferencia.CriarImediata(requisicao.IdContaOrigem, requisicao.IdContaDestino, requisicao.Valor, agora);

        return await unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var (contaOrigem, contaDestino) = await CarregarContasParaAtualizacaoOrdenadasAsync(requisicao.IdContaOrigem, requisicao.IdContaDestino, token);

            await AplicarRegrasTransferenciaAsync(transferencia, contaOrigem, contaDestino, agora, token);

            transferencias.Adicionar(transferencia);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return TransferenciaResposta.DoDominio(transferencia);
        }, ct);
    }

    public async Task<TransferenciaResposta> AgendarAsync(AgendarTransferenciaRequisicao requisicao, CancellationToken ct = default)
    {
        var agora = relogio.AgoraUtc;

        var agendadaParaInformada = requisicao.AgendadaPara ?? throw new DataAgendamentoInvalidaExcecao();
        var agendadaPara = agendadaParaInformada.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(agendadaParaInformada, DateTimeKind.Utc)
            : agendadaParaInformada.ToUniversalTime();

        var transferencia = Transferencia.CriarAgendada(requisicao.IdContaOrigem, requisicao.IdContaDestino, requisicao.Valor, agendadaPara, agora);

        _ = await contas.ObterPorIdAsync(requisicao.IdContaOrigem, ct) ?? throw new ContaNaoEncontradaExcecao(requisicao.IdContaOrigem);
        _ = await contas.ObterPorIdAsync(requisicao.IdContaDestino, ct) ?? throw new ContaNaoEncontradaExcecao(requisicao.IdContaDestino);

        transferencias.Adicionar(transferencia);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(ct);

        return TransferenciaResposta.DoDominio(transferencia);
    }

    public Task<TransferenciaResposta> CancelarAsync(Guid idTransferencia, CancellationToken ct = default) =>
        unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var transferencia = await transferencias.ObterParaAtualizacaoAsync(idTransferencia, token) ?? throw new TransferenciaNaoEncontradaExcecao(idTransferencia);

            transferencia.Cancelar();
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return TransferenciaResposta.DoDominio(transferencia);
        }, ct);

    public async Task<TransferenciaResposta> ObterAsync(Guid idTransferencia, CancellationToken ct = default)
    {
        var transferencia = await transferencias.ObterPorIdAsync(idTransferencia, ct) ?? throw new TransferenciaNaoEncontradaExcecao(idTransferencia);
        return TransferenciaResposta.DoDominio(transferencia);
    }

    public Task ExecutarAgendadaAsync(Guid idTransferencia, CancellationToken ct = default) =>
        unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var transferencia = await transferencias.ObterParaAtualizacaoAsync(idTransferencia, token) ?? throw new TransferenciaNaoEncontradaExcecao(idTransferencia);

            if (transferencia.Status != StatusTransferencia.Scheduled)
            {
                return true;
            }

            var agora = relogio.AgoraUtc;
            transferencia.MarcarComoProcessando();

            var (contaOrigem, contaDestino) = await CarregarContasParaAtualizacaoOrdenadasAsync(transferencia.IdContaOrigem, transferencia.IdContaDestino, token);
            await AplicarRegrasTransferenciaAsync(transferencia, contaOrigem, contaDestino, agora, token);

            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return true;
        }, ct);

    private async Task AplicarRegrasTransferenciaAsync(Transferencia transferencia, Conta contaOrigem, Conta contaDestino, DateTime agora, CancellationToken ct)
    {
        var motivoRejeicao = ValidarContasAtivas(contaOrigem, contaDestino)
            ?? await ValidarLimitePorHoraAsync(contaOrigem, transferencia.Valor, agora, ct)
            ?? ValidarSaldoSuficiente(contaOrigem, transferencia.Valor);

        if (motivoRejeicao is not null)
        {
            transferencia.MarcarComoFalha(agora, motivoRejeicao);
            return;
        }

        contaOrigem.Debitar(transferencia.Valor);
        contaDestino.Creditar(transferencia.Valor);
        transferencia.MarcarComoConcluida(agora);
    }

    private static string? ValidarContasAtivas(Conta contaOrigem, Conta contaDestino)
    {
        if (!contaOrigem.EstaAtiva)
        {
            return $"Conta de origem '{contaOrigem.Id}' está bloqueada/inativa.";
        }

        if (!contaDestino.EstaAtiva)
        {
            return $"Conta de destino '{contaDestino.Id}' está bloqueada/inativa.";
        }

        return null;
    }

    private static string? ValidarSaldoSuficiente(Conta contaOrigem, decimal valor) =>
        valor > contaOrigem.SaldoDisponivel
            ? $"Saldo (R$ {contaOrigem.Saldo:N2}) + cheque especial (R$ {contaOrigem.LimiteChequeEspecial:N2}) insuficiente para transferir R$ {valor:N2}."
            : null;

    private async Task<string?> ValidarLimitePorHoraAsync(Conta contaOrigem, decimal valor, DateTime agora, CancellationToken ct)
    {
        var periodo = ClassificadorPeriodoDia.Classificar(agora);

        var tentativasNaUltimaHora = await transferencias.ContarTentativasUltimaHoraAsync(contaOrigem.Id, agora, ct);
        var maxTentativas = contaOrigem.MaxTentativasPara(periodo);
        if (tentativasNaUltimaHora >= maxTentativas)
        {
            return $"Limite de {maxTentativas} tentativas por hora ({periodo}) atingido.";
        }

        var valorNaUltimaHora = await transferencias.SomarValorTransferidoUltimaHoraAsync(contaOrigem.Id, agora, ct);
        var valorMaximo = contaOrigem.LimitePara(periodo);
        if (valorNaUltimaHora + valor > valorMaximo)
        {
            return $"Limite de R$ {valorMaximo:N2} por hora ({periodo}) seria excedido (já transferido: R$ {valorNaUltimaHora:N2}).";
        }

        return null;
    }

    private async Task<(Conta ContaOrigem, Conta ContaDestino)> CarregarContasParaAtualizacaoOrdenadasAsync(Guid idContaOrigem, Guid idContaDestino, CancellationToken ct)
    {
        var (primeiroId, segundoId) = idContaOrigem.CompareTo(idContaDestino) < 0
            ? (idContaOrigem, idContaDestino)
            : (idContaDestino, idContaOrigem);

        var primeira = await contas.ObterParaAtualizacaoAsync(primeiroId, ct) ?? throw new ContaNaoEncontradaExcecao(primeiroId);
        var segunda = await contas.ObterParaAtualizacaoAsync(segundoId, ct) ?? throw new ContaNaoEncontradaExcecao(segundoId);

        return primeiroId == idContaOrigem ? (primeira, segunda) : (segunda, primeira);
    }
}
