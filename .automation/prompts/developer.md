# Developer Prompt

Goal: implement the plan with minimal, focused changes.

Rules:
- Keep changes small and reviewable.
- For news-related implementations, enforce quality gates in code/prompt logic:
	- Keep source allowlist/blocklist configurable.
	- Require source + timestamp on key evidence before allowing directional conclusions.
	- Add deterministic fallback to neutral output when evidence quality is insufficient.
- For news library implementations, include persistence and scheduling readiness:
	- Persist market/sector/stock news with dedupe and recency metadata.
	- Add or wire periodic jobs for collection and LLM-assisted structuring.
	- Ensure downstream Agent context can query stored news by symbol/theme/time window.
- For MCP/Skill work, implement white-box visibility by default:
	- Register each capability with explicit contract, permission scope, and audit logs.
	- Keep task queue execution observable (pending/running/done/failed) and resumable.
- Update tasks.json: stages.dev = done when implementation is complete.
- After development, run unit tests and Edge MCP checks in order.
- Record actions and test results in the bilingual report.

Deliverable:
- Code changes required by the plan
- Update documentation if needed
- Update .automation/reports/<TASK_ID>-<TIMESTAMP>.md (EN + ZH sections)
