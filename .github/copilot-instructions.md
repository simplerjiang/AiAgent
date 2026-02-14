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

# Continuous Rules (Required)
- During each chat, extract at least one actionable rule from observed issues and add it here.
- For split frontend/backend projects, start backend first and confirm it runs before frontend and Edge MCP tests.
- When new features are proposed or accepted, update README.md and .automation/tasks.json immediately with clear descriptions.
- Edge MCP tests must verify UI renders and interactions work, and check backend logs for errors; fix any issues found.
- If new work breaks existing features, fix them in the same task; completion requires all features to work.
- Prefer self-sufficient problem solving (reasoning and research). Only ask the user for decisions or required permissions.
- If Edge MCP cannot launch due to profile lock, use a dedicated user-data-dir under .automation/edge-profile.
- If required ports are already in use, stop the conflicting process or choose a free port, and record the chosen ports in the report.
- For Edge MCP UI checks, prefer backend-served frontend (build dist and visit backend URL); only use Vite dev server when a proxy for /api is configured, and set explicit backend URLs to avoid port conflicts.
- 在聊天过程中，每次都应该提炼一些规则并新增进去，基于你的思考与观察的问题。
- 分析项目组成，若前后端分离，先启动后端并确认可用，再启动前端与 Edge MCP。
- 沟通或新增新功能时，立即同步更新 README.md 与 .automation/tasks.json，且保证任务描述足够清晰，避免误导开发。
- Edge MCP 测试需验证 UI 正常显示与可交互，同时检查后端日志是否报错并修复。
- 新功能导致旧功能异常时需一并修复；只有全部功能正常时才算任务完成并回复。
- 尽量自我解决（思考与检索），仅在需要用户决策或权限时再求助。
- 涉及外部项目名且不明确时，先确认仓库/链接再做介绍，避免误导。
- 如果没有修改到后端代码，则不需要测试后端；如果没有修改到前端代码，则不需要测试前端。
- 如果 Edge MCP 无法启动，提示用户关闭占用的浏览器实例，或改用专用 user-data-dir（.automation/edge-profile）以避免冲突。
- 涉及第三方 API Key/Token 的配置时，必须使用环境变量或本地密钥文件，禁止明文写入仓库文件。
- 涉及密钥设置时，避免把密钥写入终端历史或日志；优先让用户本地设置环境变量后再执行。