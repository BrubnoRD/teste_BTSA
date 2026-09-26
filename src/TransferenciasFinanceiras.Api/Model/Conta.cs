using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Api.Model;

/// <summary>
/// Raiz de agregado. Cada conta pertence a uma pessoa e concentra toda regra de
/// saldo, cheque especial e limites de transferência por hora (regras 2, 3, 4 e 5).
/// </summary>
public class Conta : Entidade
{
    public string NomeTitular { get; private set; } = string.Empty;
    public decimal Saldo { get; private set; }
    public decimal LimiteChequeEspecial { get; private set; }
    public StatusConta Status { get; private set; }

    // Limite de transferência por hora, configurável por período (regra 5).
    public decimal LimiteTransferenciaDiurno { get; private set; }
    public int MaxTentativasPorHoraDiurno { get; private set; }
    public decimal LimiteTransferenciaNoturno { get; private set; }
    public int MaxTentativasPorHoraNoturno { get; private set; }

    public DateTime CriadaEm { get; private set; }

    private Conta() { } // exigido pelo EF Core

    private Conta(
        Guid id,
        string nomeTitular,
        decimal saldoInicial,
        decimal limiteChequeEspecial,
        decimal limiteTransferenciaDiurno,
        int maxTentativasPorHoraDiurno,
        decimal limiteTransferenciaNoturno,
        int maxTentativasPorHoraNoturno,
        DateTime criadaEm) : base(id)
    {
        NomeTitular = nomeTitular;
        Saldo = saldoInicial;
        LimiteChequeEspecial = limiteChequeEspecial;
        Status = StatusConta.Ativa;
        LimiteTransferenciaDiurno = limiteTransferenciaDiurno;
        MaxTentativasPorHoraDiurno = maxTentativasPorHoraDiurno;
        LimiteTransferenciaNoturno = limiteTransferenciaNoturno;
        MaxTentativasPorHoraNoturno = maxTentativasPorHoraNoturno;
        CriadaEm = criadaEm;
    }

    public static Conta Abrir(
        string nomeTitular,
        decimal saldoInicial,
        decimal limiteChequeEspecial,
        DateTime agora,
        decimal limiteTransferenciaDiurno = 5_000m,
        int maxTentativasPorHoraDiurno = 5,
        decimal limiteTransferenciaNoturno = 1_000m,
        int maxTentativasPorHoraNoturno = 3)
    {
        if (string.IsNullOrWhiteSpace(nomeTitular) || nomeTitular.Length > 200)
        {
            throw new DadosContaInvalidosExcecao("Nome do titular é obrigatório e deve ter no máximo 200 caracteres.");
        }

        GarantirValorValido(saldoInicial, "Saldo inicial", permitirZero: true);
        GarantirValorValido(limiteChequeEspecial, "Limite de cheque especial", permitirZero: true);
        GarantirValorValido(limiteTransferenciaDiurno, "Limite de transferência diurno", permitirZero: false);
        GarantirValorValido(limiteTransferenciaNoturno, "Limite de transferência noturno", permitirZero: false);

        if (maxTentativasPorHoraDiurno <= 0 || maxTentativasPorHoraNoturno <= 0)
        {
            throw new DadosContaInvalidosExcecao("O máximo de tentativas por hora deve ser maior que zero.");
        }

        return new Conta(
            Guid.NewGuid(),
            nomeTitular,
            saldoInicial,
            limiteChequeEspecial,
            limiteTransferenciaDiurno,
            maxTentativasPorHoraDiurno,
            limiteTransferenciaNoturno,
            maxTentativasPorHoraNoturno,
            agora);
    }

    private static void GarantirValorValido(decimal valor, string campo, bool permitirZero)
    {
        var valido = (permitirZero ? valor >= 0 : valor > 0)
                     && valor <= ValoresMonetarios.Maximo
                     && ValoresMonetarios.TemNoMaximoDuasCasas(valor);

        if (!valido)
        {
            var sinal = permitirZero ? "não pode ser negativo" : "deve ser maior que zero";
            throw new DadosContaInvalidosExcecao($"{campo} {sinal} e deve ter no máximo 2 casas decimais.");
        }
    }

    public bool EstaAtiva => Status == StatusConta.Ativa;

    /// <summary>Saldo + cheque especial disponível para saque/transferência.</summary>
    public decimal SaldoDisponivel => Saldo + LimiteChequeEspecial;

    public void GarantirQuePodeMovimentar()
    {
        if (!EstaAtiva)
        {
            throw new ContaInativaExcecao(Id);
        }
    }

    /// <summary>
    /// Débito da conta de origem. Pode deixar o saldo negativo, respeitando o
    /// cheque especial (regra 4). Lança se não houver saldo + limite suficiente.
    /// </summary>
    public void Debitar(decimal valor)
    {
        GarantirQuePodeMovimentar();

        if (valor > SaldoDisponivel)
        {
            throw new SaldoInsuficienteExcecao(Id);
        }

        Saldo -= valor;
    }

    /// <summary>
    /// Crédito na conta de destino. A simples soma já cobre primeiro o cheque
    /// especial utilizado antes de compor saldo positivo (ex.: -800 + 1000 = 200),
    /// então nenhuma lógica adicional de "quitação" é necessária aqui.
    /// </summary>
    public void Creditar(decimal valor)
    {
        GarantirQuePodeMovimentar();
        Saldo += valor;
    }

    public decimal LimitePara(PeriodoDia periodo) => periodo == PeriodoDia.Dia ? LimiteTransferenciaDiurno : LimiteTransferenciaNoturno;

    public int MaxTentativasPara(PeriodoDia periodo) => periodo == PeriodoDia.Dia ? MaxTentativasPorHoraDiurno : MaxTentativasPorHoraNoturno;
}
