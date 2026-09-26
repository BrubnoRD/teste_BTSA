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

        // Não modelamos chave estrangeira nem navegação para Conta de propósito: Transferencia é uma raiz de
        // agregado própria (histórico imutável), então referenciamos só o Guid da conta,
        // sem acoplar os dois agregados via propriedade de navegação do EF.
        construtor.HasIndex(t => t.IdContaOrigem);
        construtor.HasIndex(t => t.IdContaDestino);

        // Usado por ContarTentativasUltimaHoraAsync / SomarValorTransferidoUltimaHoraAsync (regra 5).
        construtor.HasIndex(t => new { t.IdContaOrigem, t.Status, t.ProcessadaEm });

        // Usado pelo processador de agendamentos para buscar o que já venceu (regra 6).
        construtor.HasIndex(t => new { t.Status, t.AgendadaPara });
    }
}
