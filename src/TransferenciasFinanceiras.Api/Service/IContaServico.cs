using TransferenciasFinanceiras.Api.Dto.Contas;

namespace TransferenciasFinanceiras.Api.Service;

public interface IContaServico
{
    Task<ContaResposta> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<ContaResposta> CriarAsync(CriarContaRequisicao requisicao, CancellationToken ct = default);

    Task<ContaResposta> BloquearAsync(Guid id, CancellationToken ct = default);

    Task<ContaResposta> AtivarAsync(Guid id, CancellationToken ct = default);
}
