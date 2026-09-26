using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface IContaRepositorio
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// Lê a conta bloqueando a linha para escrita (SELECT ... FOR UPDATE no PostgreSQL)
    /// até o fim da transação corrente. É a base do controle de concorrência pessimista
    /// usado nas transferências — ver Service/TransferenciaServico e ContaRepositorio para detalhes.
    Task<Conta?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default);

    void Adicionar(Conta conta);
}
