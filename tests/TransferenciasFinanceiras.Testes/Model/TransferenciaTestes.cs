using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Testes.Model;

public class TransferenciaTestes
{
    private static readonly DateTime Agora = new(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Origem = Guid.NewGuid();
    private static readonly Guid Destino = Guid.NewGuid();

    [Fact]
    public void Criar_ComMesmaContaDeOrigemEDestino_Lanca()
    {
        Assert.Throws<TransferenciaMesmaContaExcecao>(() => Transferencia.CriarImediata(Origem, Origem, 10m, Agora));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Criar_ComValorNaoPositivo_Lanca(decimal valor)
    {
        Assert.Throws<ValorTransferenciaInvalidoExcecao>(() => Transferencia.CriarImediata(Origem, Destino, valor, Agora));
    }

    [Fact]
    public void CriarAgendada_ComDataNoPassadoOuAgora_Lanca()
    {
        Assert.Throws<DataAgendamentoInvalidaExcecao>(() => Transferencia.CriarAgendada(Origem, Destino, 10m, Agora, Agora));
    }

    [Fact]
    public void Cancelar_TransferenciaAgendada_MudaStatusParaCancelled()
    {
        var transferencia = Transferencia.CriarAgendada(Origem, Destino, 10m, Agora.AddDays(1), Agora);

        transferencia.Cancelar();

        Assert.Equal(StatusTransferencia.Cancelled, transferencia.Status);
    }

    [Fact]
    public void Cancelar_TransferenciaJaProcessada_Lanca()
    {
        var transferencia = Transferencia.CriarAgendada(Origem, Destino, 10m, Agora.AddDays(1), Agora);
        transferencia.MarcarComoProcessando();
        transferencia.MarcarComoConcluida(Agora.AddDays(1));

        Assert.Throws<TransferenciaNaoCancelavelExcecao>(transferencia.Cancelar);
    }
}
