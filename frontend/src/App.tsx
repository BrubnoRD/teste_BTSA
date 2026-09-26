import { useState } from "react";
import { PainelConta } from "./components/PainelConta";
import { PainelTransferencia } from "./components/PainelTransferencia";
import { PainelConsultaTransferencia } from "./components/PainelConsultaTransferencia";
import type { Transferencia } from "./types";
import "./App.css";

export default function App() {
  const [ultimaTransferencia, setUltimaTransferencia] = useState<Transferencia | null>(null);

  return (
    <div className="aplicacao">
      <header>
        <h1>Transferências Financeiras</h1>
        <p>Transferências imediatas e agendadas entre contas</p>
      </header>

      <main className="grade">
        <PainelConta />
        <PainelTransferencia aoConcluir={setUltimaTransferencia} />
        <PainelConsultaTransferencia
          transferencia={ultimaTransferencia}
          aoAlterarTransferencia={setUltimaTransferencia}
        />
      </main>
    </div>
  );
}
