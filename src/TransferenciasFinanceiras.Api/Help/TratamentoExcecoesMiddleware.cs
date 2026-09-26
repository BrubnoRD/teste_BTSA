using TransferenciasFinanceiras.Api.Model.Excecoes;
using Microsoft.AspNetCore.Mvc;

namespace TransferenciasFinanceiras.Api.Help;

/// <summary>
/// Converte exceções de domínio em respostas de problema (ProblemDetails) com o status HTTP
/// correspondente, para os controladores não precisarem repetir o tratamento de erro em cada ação.
/// </summary>
public class TratamentoExcecoesMiddleware(RequestDelegate proximo, ILogger<TratamentoExcecoesMiddleware> log)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (ExcecaoDominio ex)
        {
            var status = ex switch
            {
                ContaNaoEncontradaExcecao or TransferenciaNaoEncontradaExcecao => StatusCodes.Status404NotFound,
                TransferenciaNaoCancelavelExcecao => StatusCodes.Status409Conflict,
                ContaInativaExcecao or SaldoInsuficienteExcecao => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            };

            contexto.Response.StatusCode = status;
            contexto.Response.ContentType = "application/problem+json";

            var problema = new ProblemDetails
            {
                Status = status,
                Title = ex.CodigoErro,
                Detail = ex.Message
            };

            await contexto.Response.WriteAsJsonAsync(problema);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Erro não tratado ao processar {Caminho}.", contexto.Request.Path);

            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            contexto.Response.ContentType = "application/problem+json";

            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "erro_interno",
                Detail = "Ocorreu um erro inesperado ao processar a requisição."
            };

            await contexto.Response.WriteAsJsonAsync(problema);
        }
    }
}
