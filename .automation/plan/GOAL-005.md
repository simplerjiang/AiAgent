# GOAL-005 Plan

1. Replace current ECharts stock chart renderer with `lightweight-charts` for a more professional trading UI.
2. Parse and normalize K-line data (date/open/high/low/close/volume), sort by trading date, and render candlestick + volume histogram.
3. Parse and normalize minute-line data, sort by timestamp, render area price line with baseline reference.
4. Keep existing interval tabs and placeholders while improving hover information and chart resize reliability.
5. Update unit tests for the new chart engine and verify parsing correctness.
6. Update README and automation task/state artifacts.
7. Run unit tests first, then Edge UI verification and record evidence.

## Risks
- Chart library API differences (v5) can break runtime if mocked tests do not match real API.
- Existing shell cwd drift may cause path-related command failures during scripted checks.
