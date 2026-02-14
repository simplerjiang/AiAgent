# GOAL-006 Plan

1. Extend K-line chart with two moving average overlays (MA5, MA10) using lightweight-charts line series.
2. Extend minute chart with a dedicated volume histogram subplot (minute-volume bars).
3. Keep existing chart tabs and tooltip interactions; include volume/MA info where applicable.
4. Update unit tests in `StockCharts.spec.js` to assert MA and minute volume data mapping.
5. Update README and `.automation/tasks.json` with clear status.
6. Run frontend unit tests first, then Edge UI verification.
7. Record bilingual development/test report and finalize task states.

## Risks
- Mixed minute volume field names from backend responses may produce empty bars if mapping is incomplete.
- lightweight-charts v5 series API mocking must stay aligned with implementation to avoid flaky tests.
