using TransferenciasFinanceiras.Api.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransferenciasFinanceiras.Api.Data.Configuracoes;

public class ContaConfiguracao : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> construtor)
    {
        construtor.ToTable("Contas");

        construtor.HasKey(c => c.Id);

        construtor.Property(c => c.NomeTitular)
            .IsRequired()
            .HasMaxLength(200);

        construtor.Property(c => c.Saldo).HasColumnType("decimal(18,2)");
        construtor.Property(c => c.LimiteChequeEspecial).HasColumnType("decimal(18,2)");
        construtor.Property(c => c.LimiteTransferenciaDiurno).HasColumnType("decimal(18,2)");
        construtor.Property(c => c.LimiteTransferenciaNoturno).HasColumnType("decimal(18,2)");

        construtor.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        construtor.Property(c => c.CriadaEm);
    }
}
