using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface IContaRepositorio
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<Conta?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default);

    void Adicionar(Conta conta);
}
