namespace TransferenciasFinanceiras.Api.Model;

public static class ValoresMonetarios
{
    public const decimal Maximo = 9_999_999_999_999_999.99m;

    public static bool TemNoMaximoDuasCasas(decimal valor) => decimal.Round(valor, 2) == valor;
}
