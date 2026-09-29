export type StatusConta = "Ativa" | "Bloqueada";

export type StatusTransferencia = "Scheduled" | "Processing" | "Completed" | "Failed" | "Cancelled";

export const rotulosStatus: Record<StatusConta | StatusTransferencia, string> = {
  Ativa: "Ativa",
  Bloqueada: "Bloqueada",
  Scheduled: "Agendada",
  Processing: "Processando",
  Completed: "Concluída",
  Failed: "Falhou",
  Cancelled: "Cancelada"
};

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

export interface DetalhesProblema {
  title?: string;
  detail?: string;
  status?: number;
}
