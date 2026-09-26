namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface IUnidadeDeTrabalho
{
    Task SalvarAlteracoesAsync(CancellationToken ct = default);

    /// <summary>
    /// Executa <paramref name="acao"/> dentro de uma transação de banco. Usado pelo
    /// TransferenciaServico para garantir que débito na origem e crédito no destino
    /// aconteçam de forma atômica (regra 3), com o bloqueio pessimista das contas
    /// tomado dentro da mesma transação.
    /// </summary>
    Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> acao, CancellationToken ct = default);
}
