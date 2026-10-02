using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Dto.Contas;
using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Api.Service;

public sealed class ContaServico(IContaRepositorio contas, IUnidadeDeTrabalho unidadeDeTrabalho, IProvedorDataHora relogio) : IContaServico
{
    public async Task<ContaResposta> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorIdAsync(id, ct) ?? throw new ContaNaoEncontradaExcecao(id);
        return ContaResposta.DoDominio(conta);
    }

    public async Task<ContaResposta> CriarAsync(CriarContaRequisicao requisicao, CancellationToken ct = default)
    {
        var conta = Conta.Abrir(
            requisicao.NomeTitular,
            requisicao.SaldoInicial,
            requisicao.LimiteChequeEspecial,
            relogio.AgoraUtc,
            requisicao.LimiteTransferenciaDiurno ?? 5_000m,
            requisicao.MaxTentativasPorHoraDiurno ?? 5,
            requisicao.LimiteTransferenciaNoturno ?? 1_000m,
            requisicao.MaxTentativasPorHoraNoturno ?? 3);

        contas.Adicionar(conta);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(ct);

        return ContaResposta.DoDominio(conta);
    }

    public Task<ContaResposta> BloquearAsync(Guid id, CancellationToken ct = default) =>
        AlterarStatusAsync(id, conta => conta.Bloquear(), ct);

    public Task<ContaResposta> AtivarAsync(Guid id, CancellationToken ct = default) =>
        AlterarStatusAsync(id, conta => conta.Ativar(), ct);

    private Task<ContaResposta> AlterarStatusAsync(Guid id, Action<Conta> alteracao, CancellationToken ct) =>
        unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var conta = await contas.ObterParaAtualizacaoAsync(id, token) ?? throw new ContaNaoEncontradaExcecao(id);

            alteracao(conta);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return ContaResposta.DoDominio(conta);
        }, ct);
}
