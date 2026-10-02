using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Testes.Model;

public class ContaTestes
{
    private static readonly DateTime Agora = new(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Debitar_PodeUsarChequeEspecialDeixandoSaldoNegativo()
    {
        var conta = Conta.Abrir("Ana", saldoInicial: 100m, limiteChequeEspecial: 50m, Agora);

        conta.Debitar(150m);

        Assert.Equal(-50m, conta.Saldo);
        Assert.Equal(0m, conta.SaldoDisponivel);
    }

    [Fact]
    public void Debitar_AlemDoSaldoMaisChequeEspecial_LancaENaoAlteraSaldo()
    {
        var conta = Conta.Abrir("Ana", saldoInicial: 100m, limiteChequeEspecial: 50m, Agora);

        Assert.Throws<SaldoInsuficienteExcecao>(() => conta.Debitar(150.01m));
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void Creditar_CobrePrimeiroOChequeEspecialUtilizado()
    {
        var conta = Conta.Abrir("Ana", saldoInicial: 0m, limiteChequeEspecial: 1_000m, Agora);
        conta.Debitar(800m);

        conta.Creditar(1_000m);

        Assert.Equal(200m, conta.Saldo);
    }

    [Fact]
    public void ContaBloqueada_NaoPodeDebitarNemCreditar()
    {
        var conta = Conta.Abrir("Ana", saldoInicial: 100m, limiteChequeEspecial: 0m, Agora);

        conta.Bloquear();

        Assert.Equal(StatusConta.Bloqueada, conta.Status);
        Assert.Throws<ContaInativaExcecao>(() => conta.Debitar(10m));
        Assert.Throws<ContaInativaExcecao>(() => conta.Creditar(10m));
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void Ativar_ContaBloqueada_VoltaAMovimentar()
    {
        var conta = Conta.Abrir("Ana", saldoInicial: 100m, limiteChequeEspecial: 0m, Agora);
        conta.Bloquear();

        conta.Ativar();
        conta.Debitar(10m);

        Assert.Equal(StatusConta.Ativa, conta.Status);
        Assert.Equal(90m, conta.Saldo);
    }

    [Fact]
    public void Abrir_ComChequeEspecialNegativo_Lanca()
    {
        Assert.Throws<DadosContaInvalidosExcecao>(() => Conta.Abrir("Ana", 0m, limiteChequeEspecial: -1m, Agora));
    }

    [Fact]
    public void Abrir_SemNomeDoTitular_Lanca()
    {
        Assert.Throws<DadosContaInvalidosExcecao>(() => Conta.Abrir(" ", 0m, 0m, Agora));
    }
}
