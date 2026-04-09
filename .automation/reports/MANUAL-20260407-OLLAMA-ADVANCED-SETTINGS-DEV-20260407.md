# MANUAL-20260407-OLLAMA-ADVANCED-SETTINGS Development Report

## English

- Implemented provider-level Ollama runtime settings across storage, admin DTOs, and the admin settings UI.
- Added frontend form fields for `num_ctx`, `keep_alive`, `num_predict`, `top_k`, and `top_p`, including save/load coverage and user-facing guidance that these are request-level settings.
- Switched backend Ollama calls from the OpenAI-compatible `/v1/chat/completions` path to native `/api/chat` so `options` and `keep_alive` are first-class request fields.
- Added keep-alive coercion so values such as `-1` are sent as numeric values instead of invalid duration strings.
- Added backend regression coverage in `OllamaProviderTests` and settings persistence coverage in `JsonFileLlmSettingsStoreTests`.
- Added a reusable browser validation script at `frontend/scripts/edge-check-ollama-settings.mjs` and used it in source validation after rebuilding `frontend/dist`.
- Updated local runtime settings on this machine:
  - User environment: `OLLAMA_FLASH_ATTENTION=1`
  - User environment: `OLLAMA_KV_CACHE_TYPE=q8_0`
  - User environment: `OLLAMA_KEEP_ALIVE=-1`
  - User environment: `OLLAMA_CONTEXT_LENGTH=2048`
  - Provider defaults via admin API: `ollamaNumCtx=2048`, `ollamaKeepAlive=-1`
- Validation commands and outcomes:
  - `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~OllamaProviderTests"` passed.
  - `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~JsonFileLlmSettingsStoreTests"` passed.
  - `npm --prefix .\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js` passed with 17/17 tests.
  - `node C:\Users\kong\AiAgent\frontend\scripts\edge-check-ollama-settings.mjs` passed after rebuilding `frontend/dist`; new fields were present and console error count was 0.
  - `POST http://localhost:5119/api/admin/llm/test/active` returned a normal model response with the saved active Ollama settings.
- Runtime observation after the tuning round:
  - `ollama ps` now shows `CONTEXT 2048` and `UNTIL Forever`.
  - The model still loads only partially on GPU (`75%/25% CPU/GPU`), so the machine remains VRAM-constrained.
  - Final direct decode sample was about `38.62 tok/s`; the tuning reduced reload/prompt overhead and keeps the model hot, but it did not remove the main VRAM bottleneck.

## 中文

- 已把 Ollama 请求级高级参数贯通到存储、管理员 DTO 和设置页。
- 前端设置页新增 `num_ctx`、`keep_alive`、`num_predict`、`top_k`、`top_p` 五个字段，并补了保存/回显逻辑和“这是请求级参数”的提示。
- 后端 Ollama 调用已从 OpenAI 兼容的 `/v1/chat/completions` 切到原生 `/api/chat`，这样 `options` 和 `keep_alive` 不再是旁路字段，而是正式请求参数。
- 已修复 `keep_alive = -1` 的运行态问题：如果用户输入的是纯数字字符串，现在会按数字发送，不会再被 Ollama 当成非法 duration 字符串。
- 已新增后端回归：`OllamaProviderTests` 锁定请求体写法，`JsonFileLlmSettingsStoreTests` 锁定设置持久化。
- 已新增可复用浏览器脚本 `frontend/scripts/edge-check-ollama-settings.mjs`，并在 source validation 中实际跑通。
- 这台机器上的本地 Ollama 设置已调整为：
  - 用户环境变量 `OLLAMA_FLASH_ATTENTION=1`
  - 用户环境变量 `OLLAMA_KV_CACHE_TYPE=q8_0`
  - 用户环境变量 `OLLAMA_KEEP_ALIVE=-1`
  - 用户环境变量 `OLLAMA_CONTEXT_LENGTH=2048`
  - 通过 admin API 把当前 `ollama` provider 默认值设为 `ollamaNumCtx=2048`、`ollamaKeepAlive=-1`
- 验证结果：
  - `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~OllamaProviderTests"` 通过。
  - `dotnet test C:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~JsonFileLlmSettingsStoreTests"` 通过。
  - `npm --prefix .\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js` 17/17 通过。
  - 重新 build `frontend/dist` 后，`node C:\Users\kong\AiAgent\frontend\scripts\edge-check-ollama-settings.mjs` 通过，浏览器页面可见新字段，console error 为 0。
  - `POST http://localhost:5119/api/admin/llm/test/active` 已返回正常模型响应，说明运行态主链已打通。
- 当前运行态观察：
  - `ollama ps` 现在显示 `CONTEXT 2048` 且 `UNTIL Forever`。
  - 但模型仍然只有约 `75%/25% CPU/GPU`，说明显存不足导致的部分 CPU 回退仍是主瓶颈。
  - 最终一次直连 decode 约 `38.62 tok/s`。这次调优主要解决了“参数缺失”和“模型冷热切换”问题，没有从根上解决 8GB 显存装不下当前模型的大瓶颈。