# MANUAL-20260407-OLLAMA-BENCH-EXPANSION Development Report

## English

- Completed a local Ollama benchmark round on the current RTX 5060 8GB machine and recorded the full matrix in `.automation/reports/ollama-local-benchmark-20260407.json`.
- Added explicit Ollama runtime defaults in backend storage, DTOs, provider mapping, and the admin settings UI:
  - `num_ctx=2048`
  - `keep_alive=-1`
  - `num_predict=256`
  - `temperature=0.3`
  - `top_k=64`
  - `top_p=0.95`
  - `min_p=0.0`
  - `stop=[]`
  - `think=false`
- Expanded Ollama request-level settings with `temperature`, `min_p`, `stop`, and `think`, and kept them wired through persistence, runtime serialization, and the admin UI.
- Added benchmark-aware guidance to `README.md` and updated the browser validation script so it now checks all expanded Ollama fields, not only the original five advanced settings.
- Runtime observation that directly influenced defaults:
  - Gemma 4 can return `message.thinking` with empty `message.content` unless `think` is explicitly disabled.
  - Because the app's normal path consumes `message.content`, `think=false` is now the safe default.

### Benchmark summary

- `llama3.2:3b` (`Q4_K_M`): about `152.78-155.24 tok/s`, `100% GPU`, about `2.3-2.5 GB` VRAM footprint. `num_ctx=2048` is effectively as fast as `1024` and is the better default if pure speed is the goal.
- `gemma4:e2b` (`5.1B`, `Q4_K_M`): about `33.26-35.92 tok/s`, about `75%/25% CPU/GPU`, about `7.7 GB` footprint. `num_ctx=2048` is the best balance on this machine.
- `gemma4:latest` (`8.0B`, `Q4_K_M`): about `25.62-26.48 tok/s`, about `68-69%/31-32% CPU/GPU`, about `10 GB` footprint. This is too heavy for the current 8GB-class setup and should not be the default local model.
- Final recommendation for this machine:
  - Default quality/performance balance: `gemma4:e2b` + `Q4_K_M` + `num_ctx=2048`
  - Speed-first fallback: `llama3.2:3b` + `Q4_K_M` + `num_ctx=2048`
  - Do not default to `8B Q4_K_M` here unless higher latency is explicitly acceptable.

### Validation commands and outcomes

- `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~OllamaProviderTests|FullyQualifiedName~JsonFileLlmSettingsStoreTests"` passed with `11/11` tests green.
- `npm --prefix C:\Users\kong\AiAgent\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js` passed with `18/18` tests green.
- `npm --prefix C:\Users\kong\AiAgent\frontend run build` passed.
- `node C:\Users\kong\AiAgent\frontend\scripts\edge-check-ollama-settings.mjs` passed; all expanded Ollama fields rendered and console error count was `0`.
- `GET http://localhost:5119/api/health` returned `{ "status": "ok" }`.
- `GET http://localhost:5119/api/admin/llm/settings/ollama` returned resolved runtime defaults with `model=gemma4:e2b`.
- `POST http://localhost:5119/api/admin/llm/test/active` returned a normal response: `Ollama 运行时默认设置已加载。`

## 中文

- 已在当前 RTX 5060 8GB 机器上完成一轮本地 Ollama 压测，完整结果已写入 `.automation/reports/ollama-local-benchmark-20260407.json`。
- 已把 Ollama 显式默认值贯通到后端存储、DTO、运行态请求和设置页：
  - `num_ctx=2048`
  - `keep_alive=-1`
  - `num_predict=256`
  - `temperature=0.3`
  - `top_k=64`
  - `top_p=0.95`
  - `min_p=0.0`
  - `stop=[]`
  - `think=false`
- 本轮新增并打通了更多 Ollama 请求级参数：`temperature`、`min_p`、`stop`、`think`。
- `README.md` 已补上这轮压测结论；浏览器校验脚本也已同步升级，不再只检查旧的五个高级字段。
- 这轮调优里一个关键运行态发现是：
  - Gemma 4 在未显式关闭 `think` 时，可能把内容放进 `message.thinking`，而 `message.content` 为空。
  - 由于系统主链默认读取的是 `message.content`，所以现在把 `think=false` 作为安全默认值。

### 压测结论

- `llama3.2:3b`（`Q4_K_M`）大约 `152.78-155.24 tok/s`，`100% GPU`，显存占用约 `2.3-2.5 GB`。如果只追求速度，`num_ctx=2048` 基本不比 `1024` 慢，可以直接作为小模型默认档。
- `gemma4:e2b`（`5.1B`，`Q4_K_M`）大约 `33.26-35.92 tok/s`，约 `75%/25% CPU/GPU`，占用约 `7.7 GB`。在这台机器上，`num_ctx=2048` 是质量和响应速度的最佳平衡点。
- `gemma4:latest`（`8.0B`，`Q4_K_M`）大约 `25.62-26.48 tok/s`，约 `68-69%/31-32% CPU/GPU`，占用约 `10 GB`。对当前 8GB 档显卡来说过重，不适合作为默认本地模型。
- 这台机器的最终推荐：
  - 默认质量/速度平衡档：`gemma4:e2b` + `Q4_K_M` + `num_ctx=2048`
  - 纯速度优先备选：`llama3.2:3b` + `Q4_K_M` + `num_ctx=2048`
  - 不建议把 `8B Q4_K_M` 作为这里的默认本地模型，除非你明确接受更高延迟。

### 验证结果

- `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~OllamaProviderTests|FullyQualifiedName~JsonFileLlmSettingsStoreTests"` 通过，`11/11` 全绿。
- `npm --prefix C:\Users\kong\AiAgent\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js` 通过，`18/18` 全绿。
- `npm --prefix C:\Users\kong\AiAgent\frontend run build` 通过。
- `node C:\Users\kong\AiAgent\frontend\scripts\edge-check-ollama-settings.mjs` 通过，新增字段都已渲染，console error 为 `0`。
- `GET http://localhost:5119/api/health` 返回 `{ "status": "ok" }`。
- `GET http://localhost:5119/api/admin/llm/settings/ollama` 已返回解析后的默认参数，当前模型为 `gemma4:e2b`。
- `POST http://localhost:5119/api/admin/llm/test/active` 已返回正常响应：`Ollama 运行时默认设置已加载。`