# Teste Full-Stack – Gestão de Estacionamento

Solução completa das 4 tarefas do desafio técnico, mantendo a stack original (.NET 8 + EF Core + PostgreSQL + React/Vite).

## Execução Local

### Banco PostgreSQL
1. Crie o banco `parking_test` e ajuste a `ConnectionString` em `src/backend/appsettings.json`, se necessário.
2. Execute o seed:
   ```bash
   psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
   ```

### Backend
```bash
cd src/backend
dotnet restore
dotnet run
```
API em `http://localhost:5000` (Swagger em `/swagger`).

### Frontend
```bash
cd src/frontend
npm install
npm run dev
```
App em `http://localhost:5173`.

---

## Tarefas Implementadas

### Tarefa 1 – Edição de Clientes
- Formulário reutilizável para **criar e editar** clientes (nome, telefone, endereço, mensalidade, mensalista).
- Validação de unicidade **Nome + Telefone** no backend (create e update), com índice único no PostgreSQL.
- Telefones normalizados (apenas dígitos) para evitar duplicidade por formatação (`31 99999-0001` = `31999990001`).
- Mensagens de erro claras exibidas na UI.

### Tarefa 2 – Edição de Veículos
- Edição completa de **modelo, ano e cliente associado** via formulário (sem `prompt`).
- Troca de cliente registra histórico de propriedade automaticamente.
- Lista exibe nome do cliente em vez do UUID.

### Tarefa 3 – Upload CSV
- Validação de cabeçalho e colunas obrigatórias.
- Relatório estruturado: `{ processados, inseridos, erros, detalhes[] }`.
- Cada erro informa **linha**, **motivo** e **conteúdo** da linha.
- Frontend exibe resumo numérico e tabela de erros legível.

### Tarefa 4 – Faturamento Proporcional
- Nova tabela `veiculo_cliente_historico` rastreia períodos de associação veículo ↔ cliente.
- Ao criar veículo ou trocar cliente, o histórico é atualizado automaticamente.
- Faturamento calcula dias efetivos no mês (união dos períodos) e aplica:
  ```
  valor = mensalidade × (dias_cobrados / dias_no_mês)
  ```
- Fatura inclui apenas veículos que estiveram com o cliente na competência.
- Observação registrada quando há cobrança parcial (ex.: `17/31 dias`).

---

## Decisões Técnicas

| Decisão | Motivo |
|---------|--------|
| Tabela de histórico em vez de inferir pelo `data_inclusao` | O campo `data_inclusao` do veículo só reflete o dono atual; transferências no meio do mês exigem trilha auditável. |
| União de dias (não soma por veículo) | Mensalidade é por cliente; se ele teve ao menos um veículo em um dia, esse dia conta para a cobrança. |
| Índice único `(nome, telefone)` | Garante integridade no banco e na API, alinhado ao requisito de unicidade. |
| Erros estruturados `{ mensagem }` / `CsvErroDto` | Padroniza tratamento no frontend e facilita exibição amigável. |
| `TelefoneService.Normalizar` | Evita falsos negativos na unicidade por máscaras diferentes no CSV e formulários. |
| React Query com invalidação | Mantém listas sincronizadas após mutações sem reload manual. |

---

## Estrutura

```
/src/backend        → API .NET 8
/src/frontend       → React (Vite)
/scripts/seed.sql   → Schema + seed com histórico de veículos
/scripts/exemplo.csv→ CSV de exemplo para testes de importação
```

## Cenário de Teste (Faturamento)

O seed simula a troca do veículo `ABC1D23`:
- **João Souza**: 01/08 a 17/08
- **Maria Lima**: 18/08 em diante (não mensalista → sem fatura)

Para competência `2025-08`, João mantém fatura integral pois ainda possui `BRA1A23` durante todo o mês. O veículo `ABC1D23` aparece na fatura de João pelos 17 dias em que esteve associado a ele.
