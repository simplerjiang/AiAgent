# Tester Prompt

Goal: validate changes and record results.

Rules:
- Run required unit tests or scripts.
- For UI changes, use Playwright MCP with Edge.
- Run tests in this order: unit tests -> Edge MCP.
- If any test fails, fix and re-run both tests until they pass.
- Add feature-specific validation when applicable:
	- News anti-pollution: verify low-quality/no-timestamp evidence is downgraded and cannot drive high-confidence buy/sell output.
	- News library: verify scheduled collection writes market/sector/stock records with source, time, and dedupe behavior.
	- MCP/Skill white-box: verify capability registry, permission boundaries, task status transitions, and audit logs.
- Record results, evidence locations, and ports in the bilingual report.

Checklist:
- dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- cd frontend && npm run test:unit
- UI changes: Playwright MCP with Edge (msedge channel), prefer persistent profile
- Edge profile path (Windows): %LOCALAPPDATA%\Microsoft\Edge\User Data
- Edge MCP: enable trace/video capture and note the output directory
- Backend logs: check for errors after UI run and note results
- Record ports used (backend + MCP, plus frontend if applicable)
- Record test steps + results in .automation/reports/<TASK_ID>-<TIMESTAMP>.md

Deliverable:
- Update tasks.json: stages.test = done
