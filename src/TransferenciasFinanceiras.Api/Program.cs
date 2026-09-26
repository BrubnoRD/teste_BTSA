using System.Text.Json.Serialization;
using TransferenciasFinanceiras.Api.Data;
using TransferenciasFinanceiras.Api.Help;
using TransferenciasFinanceiras.Api.Inject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers()
    .AddJsonOptions(opcoes => opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(opcoes =>
    {
        // Requisição inválida (atributos dos DTOs ou JSON malformado) segue o mesmo formato
        // dos erros de domínio: código estável em "title" e mensagem legível em "detail"
        // (é o campo que a tela exibe); o detalhamento por campo continua em "errors".
        opcoes.InvalidModelStateResponseFactory = contexto =>
        {
            var problema = new ValidationProblemDetails(contexto.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "requisicao_invalida",
                Detail = string.Join(" ", contexto.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
            };

            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        };
    });
construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Transferências Financeiras",
        Version = "v1",
        Description = "API de transferências financeiras (imediatas e agendadas) para o teste técnico."
    });
});

construtor.Services.AdicionarServicosProjeto(construtor.Configuration);

const string PoliticaCorsTela = "Tela";
construtor.Services.AddCors(opcoes =>
{
    opcoes.AddPolicy(PoliticaCorsTela, politica => politica
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var aplicacao = construtor.Build();

aplicacao.UseSwagger();
aplicacao.UseSwaggerUI();

aplicacao.UseMiddleware<TratamentoExcecoesMiddleware>();
aplicacao.UseCors(PoliticaCorsTela);
aplicacao.MapControllers();

// Aplica as migrações pendentes automaticamente ao iniciar — simplifica o "clonar e rodar"
// pedido no README de entrega, sem precisar de um passo manual de `dotnet ef database update`.
using (var escopo = aplicacao.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.Migrate();
}

aplicacao.Run();

// Torna a classe visível para um futuro projeto de testes de integração (WebApplicationFactory<Program>).
public partial class Program;
