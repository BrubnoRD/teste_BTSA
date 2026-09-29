using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;

namespace TransferenciasFinanceiras.Testes.Model;

public class ClassificadorPeriodoDiaTestes
{
    [Theory]
    [InlineData(8, 59, PeriodoDia.Noite)]
    [InlineData(9, 0, PeriodoDia.Dia)]
    [InlineData(0, 59, PeriodoDia.Dia)]
    [InlineData(1, 0, PeriodoDia.Noite)]
    [InlineData(22, 0, PeriodoDia.Dia)]
    public void Classificar_UsaHorarioDeBrasilia(int horaUtc, int minutoUtc, PeriodoDia esperado)
    {
        var instante = new DateTime(2026, 9, 26, horaUtc, minutoUtc, 0, DateTimeKind.Utc);

        Assert.Equal(esperado, ClassificadorPeriodoDia.Classificar(instante));
    }
}
