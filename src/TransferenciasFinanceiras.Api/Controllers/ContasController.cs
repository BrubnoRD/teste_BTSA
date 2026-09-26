using TransferenciasFinanceiras.Api.Dto.Contas;
using TransferenciasFinanceiras.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace TransferenciasFinanceiras.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class ContasController(IContaServico contaServico) : ControllerBase
{
    /// Consulta saldo, cheque especial, status e limites de uma conta (regra 2).
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContaResposta), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContaResposta>> ObterPorId(Guid id, CancellationToken ct)
    {
        var resultado = await contaServico.ObterPorIdAsync(id, ct);
        return Ok(resultado);
    }

    /// Rota auxiliar (não está na lista sugerida pelo enunciado) só para permitir
    /// criar contas de teste pelo Swagger ou pela tela, sem precisar inserir direto no banco.
    [HttpPost]
    [ProducesResponseType(typeof(ContaResposta), StatusCodes.Status201Created)]
    public async Task<ActionResult<ContaResposta>> Criar([FromBody] CriarContaRequisicao requisicao, CancellationToken ct)
    {
        var resultado = await contaServico.CriarAsync(requisicao, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }
}
