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

    [Fact]
    public void Finalizar_TransferenciaQueNaoEstaEmProcessamento_Lanca()
    {
        var agendada = Transferencia.CriarAgendada(Origem, Destino, 10m, Agora.AddDays(1), Agora);

        Assert.Throws<InvalidOperationException>(() => agendada.MarcarComoConcluida(Agora));
        Assert.Throws<InvalidOperationException>(() => agendada.MarcarComoFalha(Agora, "motivo"));
        Assert.Equal(StatusTransferencia.Scheduled, agendada.Status);
    }

    [Fact]
    public void Concluir_TransferenciaCancelada_Lanca()
    {
        var cancelada = Transferencia.CriarAgendada(Origem, Destino, 10m, Agora.AddDays(1), Agora);
        cancelada.Cancelar();

        Assert.Throws<InvalidOperationException>(() => cancelada.MarcarComoConcluida(Agora));
        Assert.Equal(StatusTransferencia.Cancelled, cancelada.Status);
    }

    [Fact]
    public void MarcarComoFalha_TransferenciaJaConcluida_Lanca()
    {
        var concluida = Transferencia.CriarImediata(Origem, Destino, 10m, Agora);
        concluida.MarcarComoConcluida(Agora);

        Assert.Throws<InvalidOperationException>(() => concluida.MarcarComoFalha(Agora, "motivo"));
        Assert.Equal(StatusTransferencia.Completed, concluida.Status);
    }
}
