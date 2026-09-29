import type { Conta, DetalhesProblema, Transferencia } from "../types";

const URL_BASE = import.meta.env.VITE_URL_API ?? "http://localhost:5080";

export class ErroApi extends Error {
  status: number;

  constructor(status: number, mensagem: string) {
    super(mensagem);
    this.status = status;
  }
}

async function requisitar<T>(caminho: string, opcoes?: RequestInit): Promise<T> {
  const resposta = await fetch(`${URL_BASE}${caminho}`, {
    ...opcoes,
    headers: { "Content-Type": "application/json", ...(opcoes?.headers ?? {}) }
  });

  const texto = await resposta.text();
  const dados = texto ? JSON.parse(texto) : undefined;

  if (!resposta.ok && resposta.status !== 422) {
    const problema = dados as DetalhesProblema | undefined;
    throw new ErroApi(resposta.status, problema?.detail ?? problema?.title ?? `Erro inesperado (HTTP ${resposta.status}).`);
  }

  return dados as T;
}

export interface DadosCriarConta {
  nomeTitular: string;
  saldoInicial: number;
  limiteChequeEspecial: number;
}

export interface DadosCriarTransferencia {
  idContaOrigem: string;
  idContaDestino: string;
  valor: number;
}

export interface DadosAgendarTransferencia extends DadosCriarTransferencia {
  agendadaPara: string;
}

export const api = {
  obterConta: (id: string) => requisitar<Conta>(`/api/accounts/${id}`),

  criarConta: (dados: DadosCriarConta) =>
    requisitar<Conta>(`/api/accounts`, { method: "POST", body: JSON.stringify(dados) }),

  criarTransferencia: (dados: DadosCriarTransferencia) =>
    requisitar<Transferencia>(`/api/transfers`, { method: "POST", body: JSON.stringify(dados) }),

  agendarTransferencia: (dados: DadosAgendarTransferencia) =>
    requisitar<Transferencia>(`/api/transfers/scheduled`, { method: "POST", body: JSON.stringify(dados) }),

  cancelarTransferencia: (id: string) =>
    requisitar<Transferencia>(`/api/transfers/${id}/cancel`, { method: "POST" }),

  obterTransferencia: (id: string) => requisitar<Transferencia>(`/api/transfers/${id}`)
};
