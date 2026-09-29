using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Api.Model;

public static class ClassificadorPeriodoDia
{
    public const int HoraInicioDia = 6;
    public const int HoraInicioNoite = 22;

    private static readonly TimeZoneInfo FusoHorarioBrasilia = ObterFusoHorarioBrasilia();

    public static PeriodoDia Classificar(DateTime agoraUtc)
    {
        var horaBrasilia = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, FusoHorarioBrasilia).Hour;
        return horaBrasilia >= HoraInicioDia && horaBrasilia < HoraInicioNoite ? PeriodoDia.Dia : PeriodoDia.Noite;
    }

    private static TimeZoneInfo ObterFusoHorarioBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
