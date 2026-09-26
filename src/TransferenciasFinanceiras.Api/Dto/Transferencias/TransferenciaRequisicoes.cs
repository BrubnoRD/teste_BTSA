using System.ComponentModel.DataAnnotations;
using TransferenciasFinanceiras.Api.Dto.Validacao;

namespace TransferenciasFinanceiras.Api.Dto.Transferencias;

public sealed record CriarTransferenciaRequisicao(
    [GuidNaoVazio] Guid IdContaOrigem,
    [GuidNaoVazio] Guid IdContaDestino,
    [ValorMonetario] decimal Valor);

public sealed record AgendarTransferenciaRequisicao(
    [GuidNaoVazio] Guid IdContaOrigem,
    [GuidNaoVazio] Guid IdContaDestino,
    [ValorMonetario] decimal Valor,
    // Anulável só para distinguir "campo omitido" (400) de uma data de fato informada;
    // sem isso, a omissão viraria 01/01/0001 e seria rejeitada como "data no passado".
    [Required(ErrorMessage = "O campo AgendadaPara é obrigatório.")] DateTime? AgendadaPara);
