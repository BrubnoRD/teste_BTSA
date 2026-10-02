# Transferências Financeiras

API REST + tela simples para realizar e agendar transferências financeiras entre contas, feita para o teste técnico de Desenvolvedor .NET Pleno.

## Arquitetura

Projeto único (padrão usado internamente na empresa para as APIs — Controllers/Data/Service/Dto/Model), em vez de projetos separados por camada DDD. A separação de responsabilidades continua existindo, só que como pastas dentro do mesmo projeto:

```
src/TransferenciasFinanceiras.Api/
  Controllers/      <- TransferenciasController, ContasController (rotas REST)
  Data/
    DataContext.cs        <- Contexto do EF Core (PostgreSQL / Npgsql)
    Configuracoes/        <- Mapeamento das tabelas (ContaConfiguracao, TransferenciaConfiguracao)
    Repositorios/         <- IContaRepositorio/ContaRepositorio, ITransferenciaRepositorio/TransferenciaRepositorio, IUnidadeDeTrabalho/UnidadeDeTrabalho
  Service/          <- TransferenciaServico, ContaServico (regras de negócio e casos de uso)
  Dto/              <- Objetos de requisição/resposta (Transferencias/, Contas/)
  Model/            <- Entidades ricas de domínio (Conta, Transferencia, enums, exceções)
  HostedService/    <- ProcessadorTransferenciasAgendadas (processamento de agendamentos)
  Inject/           <- Dependency.cs (registro da injeção de dependências)
  Migrations/       <- Migrações do EF Core (PostgreSQL)
  Help/             <- TratamentoExcecoesMiddleware
  Program.cs        <- Inicialização e configuração da aplicação
tests/TransferenciasFinanceiras.Testes/
  Model/            <- Testes de unidade das entidades e do ClassificadorPeriodoDia
  Service/          <- Testes de unidade do TransferenciaServico (repositórios em memória + relógio fixo)
  Integracao/       <- Testes de concorrência contra um PostgreSQL real (Testcontainers)
frontend/           <- Tela em React + Vite + TypeScript
```

Mesmo em um único projeto, as entidades em `Model/` continuam **ricas** (não são só objetos de dados sem comportamento): `Conta.Debitar`/`Creditar` e o ciclo de vida de `Transferencia` só mudam de estado através de métodos que protegem os invariantes de negócio — a camada `Service/` orquestra, mas quem decide se uma operação é válida é a própria entidade. Isso preserva o espírito de DDD/SOLID pedido no enunciado (encapsulamento das regras) dentro da convenção de pastas usada pela empresa.

> Diferença proposital em relação ao padrão de referência: não criei uma pasta `Filtros/` vazia, porque este teste não tem autenticação — ela entraria aqui se/quando a API precisar de um filtro de autorização.

### Model

- **`Conta`** concentra saldo, cheque especial, status (ativa/bloqueada) e os limites de transferência por período (dia/noite). `Debitar`/`Creditar` são os únicos pontos que alteram saldo, protegendo o invariante "não pode movimentar conta inativa" e "não pode passar do saldo + cheque especial". `Bloquear`/`Ativar` mudam o status; o serviço lê a conta com `FOR UPDATE` antes, então um bloqueio espera a transferência em andamento daquela conta terminar.
- **`Transferencia`** representa o ciclo de vida completo (`Scheduled → Processing → Completed/Failed`, ou `Cancelled`). Toda transição é protegida pela própria entidade: só se processa o que está `Scheduled`, só se cancela o que está `Scheduled` e só se conclui ou marca como falha o que está `Processing` — não existe caminho para, por exemplo, uma `Cancelled` virar `Completed`. Toda tentativa rejeitada também vira uma `Transferencia` com status `Failed`, porque a regra 5 do enunciado diz que tentativas rejeitadas contam para o limite por hora — não dava pra modelar isso sem persistir a tentativa.
- **`ClassificadorPeriodoDia`** decide se um horário é "Dia" (06:00–21:59) ou "Noite" (22:00–05:59) para aplicar o limite correto. O sistema grava e compara datas em UTC, mas a classificação converte antes para o **horário de Brasília** — sem isso, uma transferência às 19h de Brasília (22h UTC) cairia indevidamente no limite noturno.

### Service — `TransferenciaServico`

É onde as regras 3, 4, 5 e 6 do enunciado se conectam. Um único método privado (`AplicarRegrasTransferenciaAsync`) é compartilhado entre a transferência imediata e a execução de uma transferência agendada, para garantir que as duas validam exatamente as mesmas coisas na ordem: contas ativas → limite/tentativas por hora → saldo + cheque especial → débito/crédito atômico.

Decisão importante: uma transferência rejeitada **não é um erro HTTP genérico**, ela é um resultado de negócio válido e persistido. A API sempre devolve o corpo da `Transferencia`, variando só o status HTTP:

| Resultado | Status HTTP |
|---|---|
| Transferência concluída | `201 Created` |
| Transferência agendada | `201 Created` |
| Transferência rejeitada (saldo, limite, conta inativa) | `422 Unprocessable Entity` (com a `Transferencia` no corpo, `status: "Failed"`) |
| Conta/transferência inexistente | `404 Not Found` |
| Mesma conta origem/destino, valor ≤ 0, data de agendamento no passado | `400 Bad Request` (aqui não existe conta/valor coerente pra persistir uma tentativa) |
| Cancelar transferência que não está `Scheduled` | `409 Conflict` |
| Bloquear/desbloquear conta | `200 OK` (com a conta no corpo) |

Os erros seguem o formato padrão de problema do HTTP (`ProblemDetails`), com um código estável em `title` (ex.: `saldo_insuficiente`, `conta_nao_encontrada`, `transferencia_nao_cancelavel`).

### Concorrência (regra: "duas transferências simultâneas usando o mesmo saldo")

Optamos por **bloqueio pessimista** em vez de otimista (com nova tentativa). Motivo: em um sistema financeiro, prefiro que a segunda transação *espere* a primeira terminar e leia o saldo já atualizado, a deixar duas transações lerem saldo desatualizado e ter que detectar/repetir depois — é mais fácil de raciocinar e testar.

Como funciona:
1. `ContaRepositorio.ObterParaAtualizacaoAsync` lê a conta com `SELECT ... FOR UPDATE` (PostgreSQL), travando a linha até o fim da transação.
2. `TransferenciaServico` sempre trava as duas contas (origem e destino) **na mesma ordem relativa** (comparando os `Guid`, não "origem primeiro"), para que duas transferências concorrentes envolvendo o mesmo par de contas nunca fiquem esperando uma pela outra indefinidamente (impasse).
3. Tudo — leitura com bloqueio, validação de limites, débito, crédito e o registro da `Transferencia` — acontece dentro de uma única transação (`IUnidadeDeTrabalho.ExecutarEmTransacaoAsync`), garantindo atomicidade (regra 3). A transação é aberta explicitamente em `READ COMMITTED` (o padrão do PostgreSQL, mas fixado no código para não depender da configuração do servidor): em `REPEATABLE READ`, a contagem de tentativas/valor da última hora poderia ler uma "foto" antiga do banco em vez do que a transação concorrente acabou de confirmar.

4. Cancelamento e execução de um agendamento também são serializados: os dois leem a `Transferencia` com `SELECT ... FOR UPDATE` (`TransferenciaRepositorio.ObterParaAtualizacaoAsync`). Sem isso, se o cancelamento chegasse no instante em que o processador executa o agendamento, os dois leriam `Scheduled` e o último a gravar venceria — podendo terminar com o dinheiro movido e o status `Cancelled`. A execução trava a transferência antes das contas; o cancelamento só trava a transferência e a transferência imediata só as contas, então não existe ordem cruzada que gere impasse.

Como isso também resolve o limite por hora sob concorrência: como toda transferência da mesma conta de origem trava a linha dessa conta, duas transferências concorrentes da mesma origem são serializadas — a segunda só lê a contagem de tentativas/valor da última hora depois que a primeira já confirmou seu próprio registro.

### Banco de dados

As regras ficam no domínio, mas o banco é uma segunda linha de defesa — se algum código futuro (ou um script manual) tentar gravar algo inconsistente, o PostgreSQL recusa:

- **Chaves estrangeiras** de `Transferencias.IdContaOrigem` e `IdContaDestino` para `Contas`, com `ON DELETE RESTRICT` (não se apaga conta com histórico).
- **`CHECK`**: `Valor > 0`, `IdContaOrigem <> IdContaDestino`, `LimiteChequeEspecial >= 0` e `Saldo >= -LimiteChequeEspecial` (o saldo nunca passa do cheque especial).
- Valores em `numeric(18,2)`; status gravados como texto; índices para as consultas do limite por hora (`IdContaOrigem, Status, ProcessadaEm`) e do processador de agendamentos (`Status, AgendadaPara`).

### Limite de transferência por hora (regra 5)

- **Tentativas**: conta as `Transferencia`s com status `Completed` ou `Failed` cujo `ProcessadaEm` caiu na última hora — ou seja, toda tentativa que chegou a ser processada, com ou sem sucesso.
- **Valor**: soma só as `Completed` no mesmo intervalo — tentativa rejeitada não moveu dinheiro, então não deveria consumir o limite de valor (mas conta para o de tentativas).

### Agendamento (regra 6)

`ProcessadorTransferenciasAgendadas` é um serviço em segundo plano (`BackgroundService`) simples que a cada 15s busca agendamentos vencidos (`Status == Scheduled && AgendadaPara <= agora`) e chama o mesmo `AplicarRegrasTransferenciaAsync` usado na transferência imediata — ou seja, saldo/cheque especial/limites são **revalidados no momento da execução**, como pede o enunciado.

**Diferencial atendido:** isso cobre o item *"Background Worker ou Job para processamento dos agendamentos"* da seção 13 do enunciado. O processador é registrado com `AddHostedService` (em `Inject/Dependency.cs`), sobe junto com a API e não depende de nenhuma chamada externa para executar os agendamentos. Cada agendamento é executado no seu próprio escopo de injeção de dependências (e, portanto, com um `DbContext` novo, que sempre relê saldo e status do banco) e na sua própria transação — uma falha em um deles é registrada no log e não impede o processamento dos demais.

O que ficou de fora foi o outro diferencial relacionado, **mensageria**: a busca é por verificação periódica no banco, e não por uma fila.

**Várias réplicas da API:** rodar mais de uma instância é seguro — um agendamento nunca é executado duas vezes. A execução começa travando a linha da transferência com `SELECT ... FOR UPDATE` e só prossegue se o status ainda for `Scheduled`; se duas réplicas pegarem o mesmo agendamento, uma executa e a outra espera o bloqueio, encontra o status já `Completed`/`Failed` e sai sem fazer nada. A coordenação acontece no banco, não na memória da aplicação. O custo é que a segunda réplica fica esperando à toa; como evolução, `FOR UPDATE SKIP LOCKED` faria ela pular o registro travado e seguir para o próximo agendamento, distribuindo o trabalho entre as instâncias.

### Por que não há mensageria, eventos de domínio, idempotência

Foram deixados de fora combinadamente para esta entrega (são diferenciais explicitamente opcionais no enunciado). A base está desenhada para acomodá-los depois sem reescrever nada:
- **Eventos de domínio**: entrariam como uma lista de eventos em `Transferencia`/`Conta`, publicada depois do `SalvarAlteracoesAsync` na `UnidadeDeTrabalho`.
- **Idempotência**: entraria como uma tabela/coluna de chave de idempotência checada no início de `ExecutarImediataAsync`, antes de travar as contas.
- **Testes das rotas HTTP**: os testes cobrem entidades, serviço e concorrência no banco, mas não sobem a API inteira; isso viria com `WebApplicationFactory<Program>` reaproveitando o `PostgresFixture`.

## Como rodar

### Com Docker (banco + API + tela)

Pré-requisito: **Docker Desktop**. A partir da raiz do repositório:

```powershell
docker compose up -d --build
```

| Serviço | Endereço |
|---|---|
| API (Swagger) | `http://localhost:5080/swagger` |
| Tela | `http://localhost:5173` |
| PostgreSQL | `localhost:5433` (usuário `transferencias` / senha `424659`) |

O `docker-compose.yml` sobe três contêineres: `db` (PostgreSQL 16), `api` (build de `src/TransferenciasFinanceiras.Api/Dockerfile`, só inicia depois que o banco está saudável e aplica as migrações sozinha) e `frontend` (build do Vite servido pelo nginx, a partir de `frontend/Dockerfile`). Para parar: `docker compose down` (os dados continuam no volume `pg_data`; use `docker compose down -v` para apagá-los).

> Para a API do Docker usar um PostgreSQL já instalado na máquina, em vez do contêiner `db`, crie um arquivo `.env` na raiz com `CONEXAO_BANCO=Host=host.docker.internal;Port=5432;Database=transferencias_financeiras;Username=postgres;Password=postgres` e rode `docker compose up -d api`.

> Os contêineres `api` e `frontend` usam as mesmas portas (5080 e 5173) que a execução pelo Visual Studio ou `dotnet run`/`npm run dev`. Para rodar fora do Docker, pare-os antes com `docker compose stop api frontend`.

### API (sem Docker)

Pré-requisitos: **.NET 8 SDK** e um **PostgreSQL** acessível.

A string de conexão padrão (`ConnectionStrings:Padrao` em `src/TransferenciasFinanceiras.Api/appsettings.json`) aponta para um PostgreSQL instalado localmente: `localhost:5432`, usuário `postgres` / senha `postgres`. O banco `transferencias_financeiras` não precisa existir — a API cria o banco e as tabelas ao iniciar.

Sem PostgreSQL instalado, dá para subir só o banco pelo **Docker Desktop** (PostgreSQL 16 na porta `5433`, usuário `transferencias` / senha `424659`):

```powershell
docker compose up -d db
```

Nesse caso, troque a string de conexão para `Host=localhost;Port=5433;Database=transferencias_financeiras;Username=transferencias;Password=424659`.

```powershell
# a partir da raiz do repositório
dotnet restore
dotnet run --project src/TransferenciasFinanceiras.Api
```

O `Program.cs` aplica as migrações pendentes automaticamente ao iniciar (`db.Database.Migrate()`), então não é necessário rodar `dotnet ef database update` manualmente. Para usar outro banco relacional, troque o pacote `Npgsql.EntityFrameworkCore.PostgreSQL` pelo provedor correspondente, ajuste o `SELECT ... FOR UPDATE` escrito à mão em `ContaRepositorio` e gere novamente a migração.

Depois de subir, o Swagger fica em `https://localhost:5081/swagger` (ou `http://localhost:5080/swagger`).

### Tela (sem Docker)

Pré-requisitos: Node.js 20.19+ (ou 22.12+), exigido pelo Vite 8.

```powershell
cd frontend
npm install
npm run dev
```

Abre em `http://localhost:5173`. Se a API não estiver em `http://localhost:5080`, crie um `frontend/.env` com `VITE_URL_API=http://localhost:XXXX`.

A tela permite: criar uma conta de teste, consultar, bloquear e desbloquear uma conta, disparar uma transferência imediata ou agendada, e consultar/cancelar uma transferência pelo ID.

### Pelo Visual Studio (API + tela juntos)

A `TransferenciasFinanceiras.sln` tem as pastas **src** (`TransferenciasFinanceiras.Api`), **Frontend** (`frontend.esproj`, que roda o Vite) e **tests** (`TransferenciasFinanceiras.Testes`). Abra a solução, selecione o perfil **"src + Frontend"** na lista ao lado do botão de iniciar (ele vem do `TransferenciasFinanceiras.slnLaunch`) e aperte F5: a API sobe em `http://localhost:5080` e a tela em `http://localhost:5173`. É preciso ter a carga de trabalho *Node.js development* (ou *ASP.NET and web development*) instalada no Visual Studio.

## Rotas

As rotas e os status de transferência (Scheduled, Processing, Completed, Failed, Cancelled) seguem exatamente o enunciado (seções 7 e 10), por isso ficaram em inglês; o restante do código está em português.

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/transfers` | Realizar transferência imediata |
| POST | `/api/transfers/scheduled` | Agendar transferência |
| POST | `/api/transfers/{id}/cancel` | Cancelar agendamento |
| GET | `/api/transfers/{id}` | Consultar transferência |
| GET | `/api/accounts/{id}` | Consultar conta |
| POST | `/api/accounts` | *(auxiliar, fora da lista do enunciado)* criar conta de teste |
| POST | `/api/accounts/{id}/block` | *(auxiliar)* bloquear conta — passa a não enviar nem receber |
| POST | `/api/accounts/{id}/unblock` | *(auxiliar)* desbloquear conta |

## Testes

```powershell
dotnet test
```

- **Unidade** (`Model/`, `Service/`): rodam sem banco. Cobrem cheque especial, saldo insuficiente, conta bloqueada (origem, destino e bloqueio feito depois do agendamento), transições de status inválidas, limite de tentativas e de valor por hora (inclusive a janela de 1 hora e o limite noturno), fuso de Brasília, agendamento revalidado na execução e cancelamento.
- **Integração** (`Integracao/`): sobem um PostgreSQL descartável via **Testcontainers** (com as migrations reais aplicadas) e exigem o **Docker** rodando. Provam que 10 transferências simultâneas disputando o mesmo saldo não o ultrapassam, que transferências cruzadas A→B / B→A não geram impasse, que um cancelamento feito durante a execução do agendamento espera e não sobrescreve o resultado, e que as chaves estrangeiras e `CHECK`s do banco recusam dados inconsistentes.

Sem Docker, rode só os de unidade: `dotnet test --filter "Categoria!=Integracao"`.

## Migrações

```powershell
dotnet ef migrations add NomeDaMigracao --project src/TransferenciasFinanceiras.Api --startup-project src/TransferenciasFinanceiras.Api --output-dir Migrations
dotnet ef database update --project src/TransferenciasFinanceiras.Api --startup-project src/TransferenciasFinanceiras.Api
```
(O segundo comando é opcional — `dotnet run` já aplica as migrações pendentes automaticamente.)
