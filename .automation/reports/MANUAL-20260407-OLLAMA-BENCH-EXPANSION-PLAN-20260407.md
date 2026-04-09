# MANUAL-20260407-OLLAMA-BENCH-EXPANSION Plan / 计划

## Scope
- Benchmark local Ollama performance on the current RTX 5060 8GB machine across multiple model sizes and `num_ctx` values.
- Add more Ollama request-level runtime parameters into backend DTO/storage/provider and the admin settings page.
- Ensure every exposed Ollama runtime parameter has an explicit default value instead of relying on blank fields.

## Planned Actions
- Reuse installed `gemma4:e2b` and `gemma4:latest`, then add at least one smaller local comparison model.
- Capture model metadata from `ollama show` including parameter count, context length, and quantization.
- Run a repeatable local benchmark script against a fixed prompt and collect `tok/s`, load/eval durations, and `ollama ps` processor split.
- Use benchmark results to choose a machine-appropriate default combination for model size tier, quantization family, and `num_ctx`.
- Expand Ollama runtime settings with additional documented request-level fields such as `temperature`, `seed`, `min_p`, and `stop`, and wire default values through persistence and UI.

## Validation Plan
- Run the local benchmark script and record the resulting JSON report.
- Run targeted backend tests for the settings store and Ollama provider serialization.
- Run frontend unit tests for the admin settings page.
- Revalidate the backend-served admin settings page and an actual `/api/admin/llm/test/active` Ollama request.

## 中文计划
- 在当前 RTX 5060 8GB 机器上，对不同模型大小和 `num_ctx` 组合做本地 Ollama 压测。
- 在现有高级参数基础上，继续扩展更多请求级 Ollama 运行参数，并把默认值真正落到后端存储和前端设置页。
- 使用 `ollama show` 提取模型参数量、上下文上限和量化信息，再结合固定提示词压测结果，给出适合这台机器的推荐组合。
- 验证范围包括本地基准脚本、后端定向单测、前端定向单测、设置页实机检查，以及真实 `/api/admin/llm/test/active` 请求。