using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public class UnidadeDeTrabalho(DataContext db) : IUnidadeDeTrabalho
{
    public Task SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> acao, CancellationToken ct = default)
    {
        // A exclusão mútua real vem dos bloqueios explícitos (FOR UPDATE) tomados em
        // ContaRepositorio.ObterParaAtualizacaoAsync, não do nível de isolamento. O
        // ReadCommitted (já o padrão do PostgreSQL) é fixado explicitamente para que a
        // contagem de tentativas/valor da última hora sempre leia o que a transação
        // concorrente acabou de confirmar — em RepeatableRead ela leria uma "foto" antiga.
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
