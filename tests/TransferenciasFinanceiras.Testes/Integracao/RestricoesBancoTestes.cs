using Microsoft.EntityFrameworkCore;
using Npgsql;
using TransferenciasFinanceiras.Api.Model;

namespace TransferenciasFinanceiras.Testes.Integracao;

[Trait("Categoria", "Integracao")]
public class RestricoesBancoTestes(PostgresFixture banco) : IClassFixture<PostgresFixture>
{
    private static readonly DateTime Agora = new(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);

    private async Task<Conta> CriarContaAsync(decimal saldo, decimal chequeEspecial)
    {
        var conta = Conta.Abrir("Titular", saldo, chequeEspecial, Agora);

        await using var db = banco.CriarContexto();
        db.Contas.Add(conta);
        await db.SaveChangesAsync();
        return conta;
    }

    [Fact]
    public async Task Transferencia_ParaContaInexistente_EhRejeitadaPelaChaveEstrangeira()
    {
        var origem = await CriarContaAsync(saldo: 100m, chequeEspecial: 0m);

        await using var db = banco.CriarContexto();
        db.Transferencias.Add(Transferencia.CriarImediata(origem.Id, Guid.NewGuid(), 10m, Agora));

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(erro.InnerException).SqlState);
    }

    [Fact]
    public async Task Saldo_AbaixoDoChequeEspecial_EhRejeitadoPeloBanco()
    {
        var conta = await CriarContaAsync(saldo: 0m, chequeEspecial: 100m);

        await using var db = banco.CriarContexto();

        var erro = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Contas\" SET \"Saldo\" = -100.01 WHERE \"Id\" = {conta.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, erro.SqlState);
    }
}
