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
    [Required(ErrorMessage = "O campo AgendadaPara é obrigatório.")] DateTime? AgendadaPara);
