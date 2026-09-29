using TransferenciasFinanceiras.Api.Dto.Transferencias;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace TransferenciasFinanceiras.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransferenciasController(ITransferenciaServico transferenciaServico) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferenciaResposta>> Criar([FromBody] CriarTransferenciaRequisicao requisicao, CancellationToken ct)
    {
        var resultado = await transferenciaServico.ExecutarImediataAsync(requisicao, ct);
        return ResponderConformeResultado(resultado);
    }

    [HttpPost("scheduled")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferenciaResposta>> Agendar([FromBody] AgendarTransferenciaRequisicao requisicao, CancellationToken ct)
    {
        var resultado = await transferenciaServico.AgendarAsync(requisicao, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferenciaResposta>> Cancelar(Guid id, CancellationToken ct)
    {
        var resultado = await transferenciaServico.CancelarAsync(id, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferenciaResposta>> ObterPorId(Guid id, CancellationToken ct)
    {
        var resultado = await transferenciaServico.ObterAsync(id, ct);
        return Ok(resultado);
    }

    private ActionResult<TransferenciaResposta> ResponderConformeResultado(TransferenciaResposta resultado) => resultado.Status switch
    {
        StatusTransferencia.Completed => CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado),
        StatusTransferencia.Failed => UnprocessableEntity(resultado),
        _ => Ok(resultado)
    };
}
