export type StatusConta = "Ativa" | "Bloqueada";

// Nomes definidos pelo enunciado do teste (seção 7).
export type StatusTransferencia = "Scheduled" | "Processing" | "Completed" | "Failed" | "Cancelled";

// Rótulos exibidos na tela para cada status devolvido pela API.
export const rotulosStatus: Record<StatusConta | StatusTransferencia, string> = {
  Ativa: "Ativa",
  Bloqueada: "Bloqueada",
  Scheduled: "Agendada",
  Processing: "Processando",
  Completed: "Concluída",
  Failed: "Falhou",
  Cancelled: "Cancelada"
};

// Nome da classe CSS do status: o rótulo em minúsculas e sem acento (ex.: "Concluída" -> "concluida").
export function classeStatus(status: StatusConta | StatusTransferencia): string {
  return rotulosStatus[status].normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();
}

export interface Conta {
  id: string;
  nomeTitular: string;
  saldo: number;
  limiteChequeEspecial: number;
  saldoDisponivel: number;
  status: StatusConta;
  limiteTransferenciaDiurno: number;
  maxTentativasPorHoraDiurno: number;
  limiteTransferenciaNoturno: number;
  maxTentativasPorHoraNoturno: number;
}

export interface Transferencia {
  id: string;
  idContaOrigem: string;
  idContaDestino: string;
  valor: number;
  status: StatusTransferencia;
  agendadaPara: string | null;
  criadaEm: string;
  processadaEm: string | null;
  motivoFalha: string | null;
}

// Formato padrão de erro devolvido pela API (ProblemDetails); os nomes dos campos são fixos.
export interface DetalhesProblema {
  title?: string;
  detail?: string;
  status?: number;
}
