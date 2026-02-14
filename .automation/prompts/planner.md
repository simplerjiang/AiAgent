# Planner Prompt

Goal: produce a short, concrete plan for the selected task.

Rules:
- Read current requirements and related code/docs.
- Output a numbered plan with 3-7 steps.
- Identify tests that must be run.
- Note any risks or missing inputs.
- After planning, run unit tests and Edge MCP checks in order.
- Record actions and test results in the bilingual report.

Deliverable:
- Write the plan to .automation/plan/<TASK_ID>.md
- Update tasks.json: stages.plan = done
- Update .automation/reports/<TASK_ID>-<TIMESTAMP>.md (EN + ZH sections)
