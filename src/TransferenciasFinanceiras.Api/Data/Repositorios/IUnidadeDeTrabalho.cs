namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public interface IUnidadeDeTrabalho
{
    Task SalvarAlteracoesAsync(CancellationToken ct = default);

    Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> acao, CancellationToken ct = default);
}
