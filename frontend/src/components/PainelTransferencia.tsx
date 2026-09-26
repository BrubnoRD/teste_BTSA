import { useState } from "react";
import { api, ErroApi } from "../api/cliente";
import { classeStatus, rotulosStatus, type Transferencia } from "../types";

function paraValorCampoDataHora(data: Date): string {
  const doisDigitos = (n: number) => n.toString().padStart(2, "0");
  return `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}T${doisDigitos(data.getHours())}:${doisDigitos(data.getMinutes())}`;
}

export function PainelTransferencia({ aoConcluir }: { aoConcluir: (transferencia: Transferencia) => void }) {
  const [idContaOrigem, setIdContaOrigem] = useState("");
  const [idContaDestino, setIdContaDestino] = useState("");
  const [valor, setValor] = useState(100);
  const [agendar, setAgendar] = useState(false);
  const [agendadaPara, setAgendadaPara] = useState(() => {
    const daquiUmaHora = new Date(Date.now() + 60 * 60 * 1000);
    return paraValorCampoDataHora(daquiUmaHora);
  });
  const [resultado, setResultado] = useState<Transferencia | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function aoEnviar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setEnviando(true);
    setResultado(null);

    const dados = {
      idContaOrigem: idContaOrigem.trim(),
      idContaDestino: idContaDestino.trim(),
      valor
    };

    try {
      const transferencia = agendar
        ? await api.agendarTransferencia({ ...dados, agendadaPara: new Date(agendadaPara).toISOString() })
        : await api.criarTransferencia(dados);

      setResultado(transferencia);
      aoConcluir(transferencia);
    } catch (err) {
      setErro(err instanceof ErroApi ? err.message : "Falha ao processar a transferência.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <section className="cartao">
      <h2>Nova transferência</h2>

      <form onSubmit={aoEnviar} className="pilha">
        <label>
          Conta de origem
          <input value={idContaOrigem} onChange={(e) => setIdContaOrigem(e.target.value)} required />
        </label>
        <label>
          Conta de destino
          <input value={idContaDestino} onChange={(e) => setIdContaDestino(e.target.value)} required />
        </label>
        <label>
          Valor (R$)
          <input
            type="number"
            min={0.01}
            step="0.01"
            value={valor}
            onChange={(e) => setValor(Number(e.target.value))}
            required
          />
        </label>

        <label className="caixa-selecao">
          <input type="checkbox" checked={agendar} onChange={(e) => setAgendar(e.target.checked)} />
          Agendar para uma data futura
        </label>

        {agendar && (
          <label>
            Data/hora
            <input
              type="datetime-local"
              value={agendadaPara}
              onChange={(e) => setAgendadaPara(e.target.value)}
              required
            />
          </label>
        )}

        <button type="submit" disabled={enviando}>
          {agendar ? "Agendar transferência" : "Transferir agora"}
        </button>
      </form>

      {erro && <p className="erro">{erro}</p>}

      {resultado && (
        <div className={`resultado resultado-${classeStatus(resultado.status)}`}>
          <strong>Transferência {rotulosStatus[resultado.status].toLowerCase()}</strong>
          <p>
            ID: <code>{resultado.id}</code>
          </p>
          {resultado.motivoFalha && <p>Motivo: {resultado.motivoFalha}</p>}
        </div>
      )}
    </section>
  );
}
