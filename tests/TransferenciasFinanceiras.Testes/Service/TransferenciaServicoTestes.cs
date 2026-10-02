using TransferenciasFinanceiras.Api.Dto.Transferencias;
using TransferenciasFinanceiras.Api.Model;
using TransferenciasFinanceiras.Api.Model.Enums;
using TransferenciasFinanceiras.Api.Model.Excecoes;
using TransferenciasFinanceiras.Api.Service;
using TransferenciasFinanceiras.Testes.Fakes;

namespace TransferenciasFinanceiras.Testes.Service;

public class TransferenciaServicoTestes
{
    private static readonly DateTime MeioDiaBrasilia = new(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OnzeDaNoiteBrasilia = new(2026, 9, 27, 2, 0, 0, DateTimeKind.Utc);

    private readonly ContaRepositorioEmMemoria _contas = new();
    private readonly TransferenciaRepositorioEmMemoria _transferencias = new();
    private readonly RelogioFixo _relogio = new(MeioDiaBrasilia);
    private readonly TransferenciaServico _servico;

    public TransferenciaServicoTestes()
    {
        _servico = new TransferenciaServico(_contas, _transferencias, new UnidadeDeTrabalhoEmMemoria(), _relogio);
    }

    private Conta NovaConta(
        decimal saldo = 0m,
        decimal chequeEspecial = 0m,
        decimal limiteDiurno = 5_000m,
        int tentativasDiurno = 5,
        decimal limiteNoturno = 1_000m,
        int tentativasNoturno = 3)
    {
        var conta = Conta.Abrir("Titular", saldo, chequeEspecial, _relogio.AgoraUtc, limiteDiurno, tentativasDiurno, limiteNoturno, tentativasNoturno);
        _contas.Adicionar(conta);
        return conta;
    }

    private Task<TransferenciaResposta> Transferir(Conta origem, Conta destino, decimal valor) =>
        _servico.ExecutarImediataAsync(new CriarTransferenciaRequisicao(origem.Id, destino.Id, valor));

    [Fact]
    public async Task Imediata_ComSaldo_DebitaOrigemCreditaDestinoEConclui()
    {
        var origem = NovaConta(saldo: 500m);
        var destino = NovaConta();

        var resultado = await Transferir(origem, destino, 200m);

        Assert.Equal(StatusTransferencia.Completed, resultado.Status);
        Assert.Equal(300m, origem.Saldo);
        Assert.Equal(200m, destino.Saldo);
        Assert.Equal(MeioDiaBrasilia, resultado.ProcessadaEm);
    }

    [Fact]
    public async Task Imediata_UsandoChequeEspecial_Conclui()
    {
        var origem = NovaConta(saldo: 100m, chequeEspecial: 50m);
        var destino = NovaConta();

        var resultado = await Transferir(origem, destino, 150m);

        Assert.Equal(StatusTransferencia.Completed, resultado.Status);
        Assert.Equal(-50m, origem.Saldo);
    }

    [Fact]
    public async Task Imediata_SemSaldo_FalhaSemMoverDinheiroERegistraATentativa()
    {
        var origem = NovaConta(saldo: 100m, chequeEspecial: 50m);
        var destino = NovaConta();

        var resultado = await Transferir(origem, destino, 150.01m);

        Assert.Equal(StatusTransferencia.Failed, resultado.Status);
        Assert.Contains("insuficiente", resultado.MotivoFalha);
        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
        Assert.Single(_transferencias.Todas);
    }

    [Fact]
    public async Task Imediata_ContaInexistente_Lanca()
    {
        var origem = NovaConta(saldo: 100m);
        var destinoNaoCadastrado = Conta.Abrir("Fora do repositório", 0m, 0m, _relogio.AgoraUtc);

        await Assert.ThrowsAsync<ContaNaoEncontradaExcecao>(() => Transferir(origem, destinoNaoCadastrado, 10m));
    }

    [Fact]
    public async Task Imediata_OrigemBloqueada_FalhaSemMoverDinheiro()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        await new ContaServico(_contas, new UnidadeDeTrabalhoEmMemoria(), _relogio).BloquearAsync(origem.Id);

        var resultado = await Transferir(origem, destino, 10m);

        Assert.Equal(StatusTransferencia.Failed, resultado.Status);
        Assert.Contains("origem", resultado.MotivoFalha);
        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
    }

    [Fact]
    public async Task Imediata_DestinoBloqueado_FalhaSemMoverDinheiro()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        await new ContaServico(_contas, new UnidadeDeTrabalhoEmMemoria(), _relogio).BloquearAsync(destino.Id);

        var resultado = await Transferir(origem, destino, 10m);

        Assert.Equal(StatusTransferencia.Failed, resultado.Status);
        Assert.Contains("destino", resultado.MotivoFalha);
        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
    }

    [Fact]
    public async Task ExecutarAgendada_ContaBloqueadaDepoisDoAgendamento_Falha()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        var agendada = await _servico.AgendarAsync(new AgendarTransferenciaRequisicao(origem.Id, destino.Id, 50m, MeioDiaBrasilia.AddHours(1)));

        origem.Bloquear();
        _relogio.Avancar(TimeSpan.FromHours(1));
        await _servico.ExecutarAgendadaAsync(agendada.Id);

        Assert.Equal(StatusTransferencia.Failed, (await _servico.ObterAsync(agendada.Id)).Status);
        Assert.Equal(100m, origem.Saldo);
    }

    [Fact]
    public async Task LimiteDeTentativasPorHora_ContaTambemAsTentativasRejeitadas()
    {
        var origem = NovaConta(saldo: 10m, tentativasDiurno: 2);
        var destino = NovaConta();

        await Transferir(origem, destino, 1_000m);
        await Transferir(origem, destino, 5m);
        var terceira = await Transferir(origem, destino, 1m);

        Assert.Equal(StatusTransferencia.Failed, terceira.Status);
        Assert.Contains("tentativas", terceira.MotivoFalha);
        Assert.Equal(5m, origem.Saldo);
    }

    [Fact]
    public async Task LimiteDeValorPorHora_LiberaDepoisQueAJanelaDeUmaHoraPassa()
    {
        var origem = NovaConta(saldo: 1_000m, limiteDiurno: 100m);
        var destino = NovaConta();

        var primeira = await Transferir(origem, destino, 60m);
        var segunda = await Transferir(origem, destino, 50m);

        _relogio.Avancar(TimeSpan.FromHours(1));
        var terceira = await Transferir(origem, destino, 50m);

        Assert.Equal(StatusTransferencia.Completed, primeira.Status);
        Assert.Equal(StatusTransferencia.Failed, segunda.Status);
        Assert.Contains("por hora", segunda.MotivoFalha);
        Assert.Equal(StatusTransferencia.Completed, terceira.Status);
    }

    [Fact]
    public async Task ANoite_AplicaOLimiteNoturno()
    {
        var origem = NovaConta(saldo: 1_000m, limiteDiurno: 5_000m, limiteNoturno: 100m);
        var destino = NovaConta();
        var relogioNoturno = new RelogioFixo(OnzeDaNoiteBrasilia);
        var servicoNoturno = new TransferenciaServico(_contas, _transferencias, new UnidadeDeTrabalhoEmMemoria(), relogioNoturno);

        var resultado = await servicoNoturno.ExecutarImediataAsync(new CriarTransferenciaRequisicao(origem.Id, destino.Id, 200m));

        Assert.Equal(StatusTransferencia.Failed, resultado.Status);
        Assert.Contains("Noite", resultado.MotivoFalha);
    }

    [Fact]
    public async Task Agendar_DataSemFuso_ETratadaComoUtc()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        var semFuso = DateTime.SpecifyKind(MeioDiaBrasilia.AddDays(1), DateTimeKind.Unspecified);

        var resultado = await _servico.AgendarAsync(new AgendarTransferenciaRequisicao(origem.Id, destino.Id, 10m, semFuso));

        Assert.Equal(StatusTransferencia.Scheduled, resultado.Status);
        Assert.Equal(DateTimeKind.Utc, resultado.AgendadaPara!.Value.Kind);
        Assert.Equal(MeioDiaBrasilia.AddDays(1), resultado.AgendadaPara);
        Assert.Equal(100m, origem.Saldo);
    }

    [Fact]
    public async Task ExecutarAgendada_RevalidaSaldoNoMomentoDaExecucao()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        var agendada = await _servico.AgendarAsync(new AgendarTransferenciaRequisicao(origem.Id, destino.Id, 100m, MeioDiaBrasilia.AddHours(2)));

        await Transferir(origem, destino, 100m);
        _relogio.Avancar(TimeSpan.FromHours(2));
        await _servico.ExecutarAgendadaAsync(agendada.Id);

        var resultado = await _servico.ObterAsync(agendada.Id);
        Assert.Equal(StatusTransferencia.Failed, resultado.Status);
        Assert.Equal(0m, origem.Saldo);
    }

    [Fact]
    public async Task ExecutarAgendada_JaCancelada_NaoMoveDinheiro()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        var agendada = await _servico.AgendarAsync(new AgendarTransferenciaRequisicao(origem.Id, destino.Id, 50m, MeioDiaBrasilia.AddHours(1)));

        await _servico.CancelarAsync(agendada.Id);
        _relogio.Avancar(TimeSpan.FromHours(1));
        await _servico.ExecutarAgendadaAsync(agendada.Id);

        Assert.Equal(StatusTransferencia.Cancelled, (await _servico.ObterAsync(agendada.Id)).Status);
        Assert.Equal(100m, origem.Saldo);
    }

    [Fact]
    public async Task Cancelar_TransferenciaImediataJaConcluida_Lanca()
    {
        var origem = NovaConta(saldo: 100m);
        var destino = NovaConta();
        var concluida = await Transferir(origem, destino, 10m);

        await Assert.ThrowsAsync<TransferenciaNaoCancelavelExcecao>(() => _servico.CancelarAsync(concluida.Id));
    }
}
