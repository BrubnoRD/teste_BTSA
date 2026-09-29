using TransferenciasFinanceiras.Api.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransferenciasFinanceiras.Api.Data.Configuracoes;

public class TransferenciaConfiguracao : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> construtor)
    {
        construtor.ToTable("Transferencias");

        construtor.HasKey(t => t.Id);

        construtor.Property(t => t.Valor).HasColumnType("decimal(18,2)");

        construtor.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        construtor.Property(t => t.MotivoFalha).HasMaxLength(500);

        construtor.HasIndex(t => t.IdContaOrigem);
        construtor.HasIndex(t => t.IdContaDestino);

        construtor.HasIndex(t => new { t.IdContaOrigem, t.Status, t.ProcessadaEm });

        construtor.HasIndex(t => new { t.Status, t.AgendadaPara });
    }
}
