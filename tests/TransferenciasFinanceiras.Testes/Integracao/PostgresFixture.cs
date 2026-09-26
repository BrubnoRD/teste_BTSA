using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TransferenciasFinanceiras.Api.Data;
using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.Service;
using TransferenciasFinanceiras.Testes.Fakes;

namespace TransferenciasFinanceiras.Testes.Integracao;

/// <summary>
/// Sobe um PostgreSQL descartável (Docker, via Testcontainers) e aplica as migrações reais.
/// Os bloqueios FOR UPDATE só podem ser testados de verdade num banco real.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CriarContexto();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DataContext CriarContexto() =>
        new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_container.GetConnectionString()).Options);

    /// <summary>Monta o serviço como a API faz em cada requisição: um DbContext próprio por "requisição".</summary>
    public (TransferenciaServico Servico, DataContext Db) CriarServico(IProvedorDataHora relogio)
    {
        var db = CriarContexto();
        var servico = new TransferenciaServico(
            new ContaRepositorio(db),
            new TransferenciaRepositorio(db),
            new UnidadeDeTrabalho(db),
            relogio);

        return (servico, db);
    }

    public static RelogioFixo RelogioMeioDiaBrasilia() => new(new DateTime(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc));
}
