using TransferenciasFinanceiras.Api.Data;
using TransferenciasFinanceiras.Api.Data.Repositorios;
using TransferenciasFinanceiras.Api.HostedService;
using TransferenciasFinanceiras.Api.Service;
using Microsoft.EntityFrameworkCore;

namespace TransferenciasFinanceiras.Api.Inject;

/// <summary>Registro da injeção de dependências do projeto — equivalente ao Dependency.cs usado nos demais projetos da empresa.</summary>
public static class Dependency
{
    public static IServiceCollection AdicionarServicosProjeto(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var stringConexao = configuracao.GetConnectionString("Padrao")
            ?? throw new InvalidOperationException("String de conexão 'Padrao' não configurada.");

        // Dados
        servicos.AddDbContext<DataContext>(opcoes => opcoes.UseNpgsql(stringConexao));
        servicos.AddScoped<IContaRepositorio, ContaRepositorio>();
        servicos.AddScoped<ITransferenciaRepositorio, TransferenciaRepositorio>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        // Serviços
        servicos.AddSingleton<IProvedorDataHora, ProvedorDataHoraSistema>();
        servicos.AddScoped<ITransferenciaServico, TransferenciaServico>();
        servicos.AddScoped<IContaServico, ContaServico>();

        // Processamento em segundo plano
        servicos.AddHostedService<ProcessadorTransferenciasAgendadas>();

        return servicos;
    }
}
