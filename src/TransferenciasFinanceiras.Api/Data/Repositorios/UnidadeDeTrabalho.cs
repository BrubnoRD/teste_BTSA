using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public class UnidadeDeTrabalho(DataContext db) : IUnidadeDeTrabalho
{
    public Task SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> acao, CancellationToken ct = default)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        try
        {
            var resultado = await acao(ct);
            await transacao.CommitAsync(ct);
            return resultado;
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }
}
