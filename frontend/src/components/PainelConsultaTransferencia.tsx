import { useState } from "react";
import { api, ErroApi } from "../api/cliente";
import { classeStatus, rotulosStatus, type Transferencia } from "../types";

const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function PainelConsultaTransferencia({ transferencia, aoAlterarTransferencia }: {
  transferencia: Transferencia | null;
  aoAlterarTransferencia: (transferencia: Transferencia) => void;
}) {
  const [id, setId] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [ocupado, setOcupado] = useState(false);

  async function aoConsultar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setOcupado(true);
    try {
      const resultado = await api.obterTransferencia(id.trim());
      aoAlterarTransferencia(resultado);
    } catch (err) {
      setErro(err instanceof ErroApi ? err.message : "Falha ao consultar a transferência.");
    } finally {
      setOcupado(false);
    }
  }

  async function aoCancelar() {
    if (!transferencia) return;
    setErro(null);
    setOcupado(true);
    try {
      const resultado = await api.cancelarTransferencia(transferencia.id);
      aoAlterarTransferencia(resultado);
    } catch (err) {
      setErro(err instanceof ErroApi ? err.message : "Falha ao cancelar a transferência.");
    } finally {
      setOcupado(false);
    }
  }

  return (
    <section className="cartao">
      <h2>Consultar / cancelar transferência</h2>

      <form onSubmit={aoConsultar} className="linha">
        <input placeholder="ID da transferência (GUID)" value={id} onChange={(e) => setId(e.target.value)} required />
        <button type="submit" disabled={ocupado}>
          Consultar
        </button>
      </form>

      {erro && <p className="erro">{erro}</p>}

      {transferencia && (
        <>
          <dl className="detalhes">
            <dt>ID</dt>
            <dd>
              <code>{transferencia.id}</code>
            </dd>
            <dt>Status</dt>
            <dd>
              <span className={`etiqueta etiqueta-${classeStatus(transferencia.status)}`}>
                {rotulosStatus[transferencia.status]}
              </span>
            </dd>
            <dt>Origem → Destino</dt>
            <dd>
              <code>{transferencia.idContaOrigem}</code> → <code>{transferencia.idContaDestino}</code>
            </dd>
            <dt>Valor</dt>
            <dd>{moeda.format(transferencia.valor)}</dd>
            {transferencia.agendadaPara && (
              <>
                <dt>Agendada para</dt>
                <dd>{new Date(transferencia.agendadaPara).toLocaleString("pt-BR")}</dd>
              </>
            )}
            {transferencia.motivoFalha && (
              <>
                <dt>Motivo</dt>
                <dd>{transferencia.motivoFalha}</dd>
              </>
            )}
          </dl>

          {transferencia.status === "Scheduled" && (
            <button onClick={aoCancelar} disabled={ocupado} className="perigo">
              Cancelar agendamento
            </button>
          )}
        </>
      )}
    </section>
  );
}
