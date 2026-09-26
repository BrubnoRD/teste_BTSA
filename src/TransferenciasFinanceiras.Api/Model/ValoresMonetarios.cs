namespace TransferenciasFinanceiras.Api.Model;

/// <summary>Regras comuns a todo valor em reais persistido em colunas numeric(18,2).</summary>
public static class ValoresMonetarios
{
    /// <summary>Maior valor que cabe em numeric(18,2).</summary>
    public const decimal Maximo = 9_999_999_999_999_999.99m;

    public static bool TemNoMaximoDuasCasas(decimal valor) => decimal.Round(valor, 2) == valor;
}
