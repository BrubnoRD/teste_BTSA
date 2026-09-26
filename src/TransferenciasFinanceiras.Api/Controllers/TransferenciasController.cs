using TransferenciasFinanceiras.Api.Dto.Transferencias;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace TransferenciasFinanceiras.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransferenciasController(ITransferenciaServico transferenciaServico) : ControllerBase
{
    /// <summary>Realiza uma transferência imediata (regras 1 e 3).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferenciaResposta>> Criar([FromBody] CriarTransferenciaRequisicao requisicao, CancellationToken ct)
    {
        var resultado = await transferenciaServico.ExecutarImediataAsync(requisicao, ct);
        return ResponderConformeResultado(resultado);
    }

    /// <summary>Agenda uma transferência para uma data/hora futura (regra 6).</summary>
    [HttpPost("scheduled")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferenciaResposta>> Agendar([FromBody] AgendarTransferenciaRequisicao requisicao, CancellationToken ct)
    {
        var resultado = await transferenciaServico.AgendarAsync(requisicao, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }

    /// <summary>Cancela um agendamento, desde que ele ainda não tenha sido executado (regras 1 e 6).</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferenciaResposta>> Cancelar(Guid id, CancellationToken ct)
    {
        var resultado = await transferenciaServico.CancelarAsync(id, ct);
        return Ok(resultado);
    }

    /// <summary>Consulta status e dados de uma transferência (regras 1 e 7).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransferenciaResposta), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferenciaResposta>> ObterPorId(Guid id, CancellationToken ct)
    {
        var resultado = await transferenciaServico.ObterAsync(id, ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Uma transferência imediata sempre é persistida, mesmo quando rejeitada
    /// (a rejeição em si conta como tentativa — regra 5). Por isso o corpo da
    /// resposta é sempre devolvido; só o status HTTP muda conforme o resultado:
    /// 201 quando concluída, 422 quando a regra de negócio rejeitou a transferência.
    /// </summary>
    private ActionResult<TransferenciaResposta> ResponderConformeResultado(TransferenciaResposta resultado) => resultado.Status switch
    {
        StatusTransferencia.Completed => CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado),
        StatusTransferencia.Failed => UnprocessableEntity(resultado),
        _ => Ok(resultado)
    };
}
