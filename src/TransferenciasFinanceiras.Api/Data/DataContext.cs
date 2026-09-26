using TransferenciasFinanceiras.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Data;

public class DataContext(DbContextOptions<DataContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    protected override void OnModelCreating(ModelBuilder construtorModelo)
    {
        construtorModelo.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        base.OnModelCreating(construtorModelo);
    }
}
