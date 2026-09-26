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

        // Erros de forma (mesma conta / valor <= 0) são recusados antes de qualquer
        // persistência: não existe "tentativa" sem um par de contas coerente (regra 3).
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

        // O PostgreSQL (timestamp with time zone) só aceita DateTime em UTC. Data sem fuso
        // informado é tratada como UTC, igual às demais datas do sistema.
        // AgendadaPara é [Required] no DTO; nulo aqui só se a validação da API for contornada.
        var agendadaParaInformada = requisicao.AgendadaPara ?? throw new DataAgendamentoInvalidaExcecao();
        var agendadaPara = agendadaParaInformada.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(agendadaParaInformada, DateTimeKind.Utc)
            : agendadaParaInformada.ToUniversalTime();

        var transferencia = Transferencia.CriarAgendada(requisicao.IdContaOrigem, requisicao.IdContaDestino, requisicao.Valor, agendadaPara, agora);

        // Não há necessidade de bloqueio aqui: nenhum saldo é movimentado no agendamento,
        // só confirmamos que as contas existem. Tudo será revalidado na execução (regra 6).
        _ = await contas.ObterPorIdAsync(requisicao.IdContaOrigem, ct) ?? throw new ContaNaoEncontradaExcecao(requisicao.IdContaOrigem);
        _ = await contas.ObterPorIdAsync(requisicao.IdContaDestino, ct) ?? throw new ContaNaoEncontradaExcecao(requisicao.IdContaDestino);

        transferencias.Adicionar(transferencia);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(ct);

        return TransferenciaResposta.DoDominio(transferencia);
    }

    public Task<TransferenciaResposta> CancelarAsync(Guid idTransferencia, CancellationToken ct = default) =>
        unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            // Mesmo bloqueio usado em ExecutarAgendadaAsync: se o processador estiver executando
            // este agendamento agora, o cancelamento espera e então vê o status já atualizado.
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
            // Transferência travada antes das contas; o cancelamento só trava a transferência
            // e a transferência imediata só as contas, então não há ordem cruzada (impasse).
            var transferencia = await transferencias.ObterParaAtualizacaoAsync(idTransferencia, token) ?? throw new TransferenciaNaoEncontradaExcecao(idTransferencia);

            if (transferencia.Status != StatusTransferencia.Scheduled)
            {
                // Já foi cancelada ou processada (execução duplicada do processador) — nada a fazer.
                return true;
            }

            var agora = relogio.AgoraUtc;
            transferencia.MarcarComoProcessando();

            var (contaOrigem, contaDestino) = await CarregarContasParaAtualizacaoOrdenadasAsync(transferencia.IdContaOrigem, transferencia.IdContaDestino, token);
            await AplicarRegrasTransferenciaAsync(transferencia, contaOrigem, contaDestino, agora, token);

            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return true;
        }, ct);

    /// Núcleo das regras 3, 4 e 5. Compartilhado entre transferência imediata e a
    /// execução de uma transferência agendada, para que as duas validem exatamente
    /// as mesmas coisas (contas ativas, limite/tentativas por hora, saldo + cheque
    /// especial) antes de debitar/creditar de forma atômica.
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

    /// Carrega as duas contas com bloqueio pessimista (SELECT ... FOR UPDATE) sempre na
    /// mesma ordem — pelo Guid, não por origem/destino — para que duas transferências
    /// concorrentes envolvendo o mesmo par de contas nunca se travem mutuamente
    /// (impasse). Isso também serializa transferências concorrentes que compartilham
    /// a mesma conta de origem, o que é o que garante a correção do cenário "duas
    /// transferências simultâneas utilizando o mesmo saldo".
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
