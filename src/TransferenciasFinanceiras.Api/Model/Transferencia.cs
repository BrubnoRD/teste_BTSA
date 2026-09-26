using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;

namespace TransferenciasFinanceiras.Api.Model;

/// <summary>
/// Representa uma transferência imediata ou agendada e seu ciclo de vida (regras 1, 6 e 7).
/// Toda tentativa — inclusive as rejeitadas — vira um registro aqui, pois elas contam
/// para o limite de tentativas por hora (regra 5).
/// </summary>
public class Transferencia : Entidade
{
    public Guid IdContaOrigem { get; private set; }
    public Guid IdContaDestino { get; private set; }
    public decimal Valor { get; private set; }
    public StatusTransferencia Status { get; private set; }

    /// <summary>Nulo para transferências imediatas. Preenchido para agendamentos.</summary>
    public DateTime? AgendadaPara { get; private set; }

    public DateTime CriadaEm { get; private set; }
    public DateTime? ProcessadaEm { get; private set; }
    public string? MotivoFalha { get; private set; }

    private Transferencia() { } // exigido pelo EF Core

    private Transferencia(
        Guid id,
        Guid idContaOrigem,
        Guid idContaDestino,
        decimal valor,
        StatusTransferencia status,
        DateTime? agendadaPara,
        DateTime criadaEm) : base(id)
    {
        IdContaOrigem = idContaOrigem;
        IdContaDestino = idContaDestino;
        Valor = valor;
        Status = status;
        AgendadaPara = agendadaPara;
        CriadaEm = criadaEm;
    }

    private static void ValidarSolicitacao(Guid idContaOrigem, Guid idContaDestino, decimal valor)
    {
        if (idContaOrigem == idContaDestino)
        {
            throw new TransferenciaMesmaContaExcecao();
        }

        if (valor <= 0 || valor > ValoresMonetarios.Maximo || !ValoresMonetarios.TemNoMaximoDuasCasas(valor))
        {
            throw new ValorTransferenciaInvalidoExcecao();
        }
    }

    public static Transferencia CriarImediata(Guid idContaOrigem, Guid idContaDestino, decimal valor, DateTime agora)
    {
        ValidarSolicitacao(idContaOrigem, idContaDestino, valor);
        return new Transferencia(Guid.NewGuid(), idContaOrigem, idContaDestino, valor, StatusTransferencia.Processing, null, agora);
    }

    public static Transferencia CriarAgendada(Guid idContaOrigem, Guid idContaDestino, decimal valor, DateTime agendadaPara, DateTime agora)
    {
        ValidarSolicitacao(idContaOrigem, idContaDestino, valor);

        if (agendadaPara <= agora)
        {
            throw new DataAgendamentoInvalidaExcecao();
        }

        return new Transferencia(Guid.NewGuid(), idContaOrigem, idContaDestino, valor, StatusTransferencia.Scheduled, agendadaPara, agora);
    }

    public void MarcarComoProcessando()
    {
        if (Status != StatusTransferencia.Scheduled)
        {
            throw new InvalidOperationException($"Só é possível processar uma transferência agendada. Status atual: {Status}.");
        }

        Status = StatusTransferencia.Processing;
    }

    public void MarcarComoConcluida(DateTime processadaEm)
    {
        Status = StatusTransferencia.Completed;
        ProcessadaEm = processadaEm;
        MotivoFalha = null;
    }

    public void MarcarComoFalha(DateTime processadaEm, string motivo)
    {
        Status = StatusTransferencia.Failed;
        ProcessadaEm = processadaEm;
        MotivoFalha = motivo;
    }

    /// <summary>Cancelamento só é permitido enquanto a transferência estiver agendada (regra 1 e 6).</summary>
    public void Cancelar()
    {
        if (Status != StatusTransferencia.Scheduled)
        {
            throw new TransferenciaNaoCancelavelExcecao(Id);
        }

        Status = StatusTransferencia.Cancelled;
    }
}
