- [x] Verify that the copilot-instructions.md file in the .github directory is created.

- [x] Clarify Project Requirements

- [x] Scaffold the Project

- [x] Customize the Project

- [x] Install Required Extensions

- [x] Compile the Project

- [x] Create and Run Task

- [ ] Launch the Project

- [x] Ensure Documentation is Complete
- Every change must be validated by running a relevant unit test or script and reporting the result.
- After each change, update or add unit tests when applicable, or run a relevant script test, and report the result.
- Work through each checklist item systematically.
- Keep communication concise and focused.
- Follow development best practices.

# Multi-Agent Automation (Local)
- Use the automation workflow in .automation/README.md.
- Always keep tasks.json and state.json in sync with progress.
- Use the prompts in .automation/prompts for plan, dev, and test.

# Bilingual Reporting (Required)
- After planning and after development, write a bilingual report (EN + ZH) in .automation/reports.
- English is for agents, Chinese is for the user.
- Record all actions, test commands, results, and any issues.

# Testing Order (Required)
- Run unit tests first, then Edge MCP checks.
- If any test fails, fix and re-run both until they pass.
- Use Playwright MCP with Edge (msedge) and the existing Edge profile when possible.

# Git Workflow (Required)
- After tests pass, commit and push.
- If no remote is configured, request the remote URL before pushing.
- Keep commits focused and include report updates.
