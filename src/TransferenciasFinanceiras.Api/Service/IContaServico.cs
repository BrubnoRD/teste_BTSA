using TransferenciasFinanceiras.Api.Dto.Contas;

namespace TransferenciasFinanceiras.Api.Service;

public interface IContaServico
{
    Task<ContaResposta> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// Rota auxiliar (fora da lista sugerida no enunciado) só para permitir criar contas de teste.
    Task<ContaResposta> CriarAsync(CriarContaRequisicao requisicao, CancellationToken ct = default);
}
