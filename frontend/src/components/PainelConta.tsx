import { useState } from "react";
import { api, ErroApi, type DadosCriarConta } from "../api/cliente";
import { classeStatus, rotulosStatus, type Conta } from "../types";

const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function PainelConta() {
  const [idConsulta, setIdConsulta] = useState("");
  const [conta, setConta] = useState<Conta | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [consultando, setConsultando] = useState(false);

  const [formulario, setFormulario] = useState<DadosCriarConta>({
    nomeTitular: "",
    saldoInicial: 1000,
    limiteChequeEspecial: 500
  });
  const [criando, setCriando] = useState(false);

  async function aoConsultar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setConsultando(true);
    try {
      const resultado = await api.obterConta(idConsulta.trim());
      setConta(resultado);
    } catch (err) {
      setConta(null);
      setErro(err instanceof ErroApi ? err.message : "Falha ao consultar a conta.");
    } finally {
      setConsultando(false);
    }
  }

  async function aoCriar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setCriando(true);
    try {
      const resultado = await api.criarConta(formulario);
      setConta(resultado);
      setIdConsulta(resultado.id);
    } catch (err) {
      setErro(err instanceof ErroApi ? err.message : "Falha ao criar a conta.");
    } finally {
      setCriando(false);
    }
  }

  return (
    <section className="cartao">
      <h2>Contas</h2>

      <form onSubmit={aoConsultar} className="linha">
        <input
          placeholder="ID da conta (GUID)"
          value={idConsulta}
          onChange={(e) => setIdConsulta(e.target.value)}
          required
        />
        <button type="submit" disabled={consultando}>
          Consultar
        </button>
      </form>

      <details>
        <summary>Criar conta de teste</summary>
        <form onSubmit={aoCriar} className="pilha">
          <label>
            Nome do titular
            <input
              value={formulario.nomeTitular}
              onChange={(e) => setFormulario({ ...formulario, nomeTitular: e.target.value })}
              required
            />
          </label>
          <label>
            Saldo inicial (R$)
            <input
              type="number"
              step="0.01"
              value={formulario.saldoInicial}
              onChange={(e) => setFormulario({ ...formulario, saldoInicial: Number(e.target.value) })}
            />
          </label>
          <label>
            Limite de cheque especial (R$)
            <input
              type="number"
              step="0.01"
              value={formulario.limiteChequeEspecial}
              onChange={(e) => setFormulario({ ...formulario, limiteChequeEspecial: Number(e.target.value) })}
            />
          </label>
          <button type="submit" disabled={criando}>
            Criar conta
          </button>
        </form>
      </details>

      {erro && <p className="erro">{erro}</p>}

      {conta && (
        <dl className="detalhes">
          <dt>ID</dt>
          <dd>
            <code>{conta.id}</code>
          </dd>
          <dt>Titular</dt>
          <dd>{conta.nomeTitular}</dd>
          <dt>Status</dt>
          <dd>
            <span className={`etiqueta etiqueta-${classeStatus(conta.status)}`}>{rotulosStatus[conta.status]}</span>
          </dd>
          <dt>Saldo</dt>
          <dd>{moeda.format(conta.saldo)}</dd>
          <dt>Cheque especial</dt>
          <dd>{moeda.format(conta.limiteChequeEspecial)}</dd>
          <dt>Disponível para transferir</dt>
          <dd>{moeda.format(conta.saldoDisponivel)}</dd>
          <dt>Limite dia / noite</dt>
          <dd>
            {moeda.format(conta.limiteTransferenciaDiurno)} ({conta.maxTentativasPorHoraDiurno} tentativas) /{" "}
            {moeda.format(conta.limiteTransferenciaNoturno)} ({conta.maxTentativasPorHoraNoturno} tentativas)
          </dd>
        </dl>
      )}
    </section>
  );
}
