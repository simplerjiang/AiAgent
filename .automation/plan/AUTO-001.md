# Plan - AUTO-001

1) Define the next automation features to add (Playwright MCP Edge usage, logging format, rollback rules).
2) Add a concise Edge MCP setup guide and a tester checklist for UI validation.
3) Wire a lightweight "plan/dev/test" progression in tasks.json and document how to advance it.
4) Validate the workflow by running the automation scripts and required tests.
5) Record results in the run log and keep the task status in sync.

## Tests
- dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- cd frontend && npm run test:unit
- Playwright MCP (Edge) if UI changes are introduced
