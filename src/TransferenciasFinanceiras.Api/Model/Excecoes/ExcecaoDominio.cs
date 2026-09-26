namespace TransferenciasFinanceiras.Api.Model.Excecoes;

/// <summary>
/// Base para toda violação de regra de negócio. A API mapeia o tipo concreto
/// para um status HTTP e um código de erro estável (ver Help/TratamentoExcecoesMiddleware).
/// </summary>
public abstract class ExcecaoDominio : Exception
{
    protected ExcecaoDominio(string codigoErro, string mensagem) : base(mensagem)
    {
        CodigoErro = codigoErro;
    }

    public string CodigoErro { get; }
}

public sealed class ContaNaoEncontradaExcecao(Guid idConta)
    : ExcecaoDominio("conta_nao_encontrada", $"Conta '{idConta}' não encontrada.");

public sealed class TransferenciaNaoEncontradaExcecao(Guid idTransferencia)
    : ExcecaoDominio("transferencia_nao_encontrada", $"Transferência '{idTransferencia}' não encontrada.");

public sealed class ContaInativaExcecao(Guid idConta)
    : ExcecaoDominio("conta_inativa", $"Conta '{idConta}' está bloqueada/inativa e não pode enviar ou receber transferências.");

public sealed class TransferenciaMesmaContaExcecao()
    : ExcecaoDominio("transferencia_mesma_conta", "Conta de origem e destino devem ser diferentes.");

public sealed class ValorTransferenciaInvalidoExcecao()
    : ExcecaoDominio("valor_transferencia_invalido", "O valor da transferência deve ser maior que zero e ter no máximo 2 casas decimais.");

public sealed class DadosContaInvalidosExcecao(string mensagem)
    : ExcecaoDominio("dados_conta_invalidos", mensagem);

public sealed class SaldoInsuficienteExcecao(Guid idConta)
    : ExcecaoDominio("saldo_insuficiente", $"Conta '{idConta}' não possui saldo + cheque especial suficiente.");

public sealed class DataAgendamentoInvalidaExcecao()
    : ExcecaoDominio("data_agendamento_invalida", "A data de agendamento deve ser futura.");

public sealed class TransferenciaNaoCancelavelExcecao(Guid idTransferencia)
    : ExcecaoDominio("transferencia_nao_cancelavel", $"Transferência '{idTransferencia}' não está agendada e não pode ser cancelada.");
