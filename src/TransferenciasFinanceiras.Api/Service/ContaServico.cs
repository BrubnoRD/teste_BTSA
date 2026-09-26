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
}
