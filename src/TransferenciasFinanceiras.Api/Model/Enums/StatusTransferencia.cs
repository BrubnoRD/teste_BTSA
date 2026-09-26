namespace TransferenciasFinanceiras.Api.Model.Enums;

/// <summary>Nomes definidos pelo enunciado do teste (seção 7), por isso mantidos em inglês no contrato da API.</summary>
public enum StatusTransferencia
{
    Scheduled = 1,  // agendada
    Processing = 2, // em processamento
    Completed = 3,  // concluída
    Failed = 4,     // falhou
    Cancelled = 5   // cancelada
}
