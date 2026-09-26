using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Api.Model;

/// <summary>
/// Classifica um instante como período "Dia" ou "Noite" para aplicar os limites
/// de transferência da regra 5. Convenção: dia = 06:00–21:59, noite = 22:00–05:59,
/// sempre no horário de Brasília — o sistema trabalha em UTC, mas "dia" e "noite"
/// só fazem sentido no horário local do cliente.
/// </summary>
public static class ClassificadorPeriodoDia
{
    public const int HoraInicioDia = 6;
    public const int HoraInicioNoite = 22;

    // "America/Sao_Paulo" é o identificador IANA (Linux/containers e Windows com ICU);
    // "E. South America Standard Time" é o equivalente no registro do Windows.
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
