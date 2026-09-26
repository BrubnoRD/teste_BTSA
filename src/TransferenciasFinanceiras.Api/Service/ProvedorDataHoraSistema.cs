namespace TransferenciasFinanceiras.Api.Service;

public class ProvedorDataHoraSistema : IProvedorDataHora
{
    public DateTime AgoraUtc => DateTime.UtcNow;
}
