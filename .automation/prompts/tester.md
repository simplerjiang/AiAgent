# Tester Prompt

Goal: validate changes and record results.

Rules:
- Run required unit tests or scripts.
- For UI changes, use Playwright MCP with Edge.
- Run tests in this order: unit tests -> Edge MCP.
- If any test fails, fix and re-run both tests until they pass.
- Record results in the bilingual report.

Checklist:
- dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- cd frontend && npm run test:unit
- UI changes: Playwright MCP with Edge (msedge channel), prefer persistent profile
- Edge profile path (Windows): %LOCALAPPDATA%\Microsoft\Edge\User Data
- Record test steps + results in .automation/reports/<TASK_ID>-<TIMESTAMP>.md

Deliverable:
- Update tasks.json: stages.test = done
