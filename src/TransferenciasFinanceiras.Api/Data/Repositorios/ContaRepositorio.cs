using TransferenciasFinanceiras.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public class ContaRepositorio(DataContext db) : IContaRepositorio
{
    public Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Contas.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conta?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default) =>
        db.Contas
            .FromSqlInterpolated($"SELECT * FROM \"Contas\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public void Adicionar(Conta conta) => db.Contas.Add(conta);
}
