using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Testes.Model;

public class ClassificadorPeriodoDiaTestes
{
    // Brasília = UTC-3 (sem horário de verão desde 2019). As horas abaixo estão em UTC.
    [Theory]
    [InlineData(8, 59, PeriodoDia.Noite)]  // 05:59 em Brasília
    [InlineData(9, 0, PeriodoDia.Dia)]     // 06:00 em Brasília
    [InlineData(0, 59, PeriodoDia.Dia)]    // 21:59 em Brasília
    [InlineData(1, 0, PeriodoDia.Noite)]   // 22:00 em Brasília
    [InlineData(22, 0, PeriodoDia.Dia)]    // 19:00 em Brasília — seria "noite" se a conta fosse feita em UTC
    public void Classificar_UsaHorarioDeBrasilia(int horaUtc, int minutoUtc, PeriodoDia esperado)
    {
        var instante = new DateTime(2026, 9, 26, horaUtc, minutoUtc, 0, DateTimeKind.Utc);

        Assert.Equal(esperado, ClassificadorPeriodoDia.Classificar(instante));
    }
}
