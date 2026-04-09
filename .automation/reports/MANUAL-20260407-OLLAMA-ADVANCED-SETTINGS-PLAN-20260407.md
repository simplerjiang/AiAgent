# MANUAL-20260407-OLLAMA-ADVANCED-SETTINGS Plan Report

## English

- Task: Add advanced Ollama runtime settings to the existing admin LLM settings flow, wire them into backend Ollama requests, tune local Ollama defaults on this Windows machine, and provide deployment-platform guidance.
- Accepted scope:
  - Persist and return provider-level Ollama request options in admin settings.
  - Update the frontend settings page to edit and save those options.
  - Change backend Ollama calls to a native API path that formally supports `options` and `keep_alive`.
  - Apply conservative local Ollama environment changes suitable for the current RTX 5060 8GB machine.
  - Run targeted backend/frontend tests and a direct Ollama throughput verification.
- Constraints and decisions:
  - Keep the UI limited to per-request parameters; server-wide knobs such as Flash Attention, KV cache type, and concurrency are handled as local Ollama environment settings instead of misleading per-provider fields.
  - Avoid delegating file creation to subagents due to known persistence issues; subagents are used only for read-only exploration.
- Planned validation:
  - `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~JsonFileLlmSettingsStoreTests|FullyQualifiedName~OllamaProviderTests"`
  - `npm --prefix .\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js`
  - Direct `Invoke-RestMethod` call to local Ollama for runtime verification after local settings update.

## 中文

- 任务：在现有 `LLM 设置` 页中加入 Ollama 高级运行参数，确保后端请求真正带上这些参数，并在这台 Windows 机器上同步调整本地 Ollama 配置，最后给出是否迁移到其他部署平台的建议。
- 已确认范围：
  - 在管理员设置中持久化并返回 Ollama 请求级参数。
  - 在前端设置页中新增这些参数的编辑与保存入口。
  - 后端 Ollama 调用切换到正式支持 `options` 与 `keep_alive` 的原生接口。
  - 按当前 RTX 5060 8GB 机器实际约束，写入一组偏保守但可落地的本机 Ollama 环境变量。
  - 跑定向后端/前端测试，并做一次本地 Ollama 直连验证。
- 约束与决策：
  - 页面里只放“单次请求参数”；Flash Attention、KV cache、并发数这类服务级参数不伪装成 provider 字段，而是通过本机 Ollama 环境变量处理。
  - 由于已知子代理写文件会丢失，本轮所有文件创建与编辑都在主会话完成；子代理只做只读梳理。
- 计划验证命令：
  - `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~JsonFileLlmSettingsStoreTests|FullyQualifiedName~OllamaProviderTests"`
  - `npm --prefix .\frontend run test:unit -- src/modules/admin/AdminLlmSettings.spec.js`
  - 本地 Ollama 设置更新后，再用 `Invoke-RestMethod` 直连测一次运行态。