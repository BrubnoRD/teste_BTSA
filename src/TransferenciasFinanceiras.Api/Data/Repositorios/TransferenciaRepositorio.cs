using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Data.Repositorios;

public class TransferenciaRepositorio(DataContext db) : ITransferenciaRepositorio
{
    public Task<Transferencia?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Transferencias.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transferencia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken ct = default) =>
        db.Transferencias
            .FromSqlInterpolated($"SELECT * FROM \"Transferencias\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public void Adicionar(Transferencia transferencia) => db.Transferencias.Add(transferencia);

    public Task<int> ContarTentativasUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default)
    {
        var inicioJanela = agora.AddHours(-1);

        return db.Transferencias.CountAsync(
            t => t.IdContaOrigem == idContaOrigem
                 && (t.Status == StatusTransferencia.Completed || t.Status == StatusTransferencia.Failed)
                 && t.ProcessadaEm != null
                 && t.ProcessadaEm > inicioJanela
                 && t.ProcessadaEm <= agora,
            ct);
    }

    public Task<decimal> SomarValorTransferidoUltimaHoraAsync(Guid idContaOrigem, DateTime agora, CancellationToken ct = default)
    {
        var inicioJanela = agora.AddHours(-1);

        return db.Transferencias
            .Where(t => t.IdContaOrigem == idContaOrigem
                        && t.Status == StatusTransferencia.Completed
                        && t.ProcessadaEm != null
                        && t.ProcessadaEm > inicioJanela
                        && t.ProcessadaEm <= agora)
            .SumAsync(t => t.Valor, ct);
    }

    public async Task<IReadOnlyList<Guid>> ObterIdsAgendamentosVencidosAsync(DateTime agora, CancellationToken ct = default) =>
        await db.Transferencias
            .Where(t => t.Status == StatusTransferencia.Scheduled && t.AgendadaPara != null && t.AgendadaPara <= agora)
            .OrderBy(t => t.AgendadaPara)
            .Select(t => t.Id)
            .ToListAsync(ct);
}
