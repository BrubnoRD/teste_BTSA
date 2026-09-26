namespace TransferenciasFinanceiras.Api.Service;

/// <summary>Abstrai "agora" para permitir testar cenários de dia/noite e agendamento sem depender do relógio real.</summary>
public interface IProvedorDataHora
{
    DateTime AgoraUtc { get; }
}
