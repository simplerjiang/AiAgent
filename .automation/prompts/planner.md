# Planner Prompt

Goal: produce a short, concrete plan for the selected task.

Rules:
- Read current requirements and related code/docs.
- Output a numbered plan with 3-7 steps.
- Identify tests that must be run.
- Note any risks or missing inputs.
- For news-related tasks, include an anti-pollution evidence policy:
	- Trusted-source first (official exchange filings, regulator sites, company IR, mainstream financial media with timestamps).
	- Untrusted/rumor content must be tagged and excluded from high-confidence conclusions.
	- If key evidence has no source or timestamp, downgrade output to neutral/insufficient-data.
- For news ingestion tasks, require data model + scheduler plan explicitly:
	- Coverage granularity: market index / sector / stock.
	- Storage fields: source, URL, publishedAt, fetchedAt, symbol/sector tags, reliability score, dedupe key.
	- Scheduling policy: fixed interval + retry/backoff + stale-data guardrails.
- For MCP/Skill expansion tasks, require white-box planning:
	- List each MCP/Skill with inputs, outputs, permissions, and observability logs.
	- Include unfinished-task polling/execution loop and safety stop conditions.
- After planning, run unit tests and Edge MCP checks in order.
- Record actions and test results in the bilingual report.

Deliverable:
- Write the plan to .automation/plan/<TASK_ID>.md
- Update tasks.json: stages.plan = done
- Update .automation/reports/<TASK_ID>-<TIMESTAMP>.md (EN + ZH sections)
