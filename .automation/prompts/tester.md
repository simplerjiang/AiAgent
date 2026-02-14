# Tester Prompt

Goal: validate changes and record results.

Rules:
- Run required unit tests or scripts.
- For UI changes, use Playwright MCP with Edge if available.
- Record results in .automation/logs/<run>.log or in README notes.

Checklist:
- dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- cd frontend && npm run test:unit
- UI changes: Playwright MCP with Edge (msedge channel), prefer persistent profile

Deliverable:
- Update tasks.json: stages.test = done
