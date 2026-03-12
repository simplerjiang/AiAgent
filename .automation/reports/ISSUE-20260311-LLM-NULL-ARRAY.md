# ISSUE-20260311 LLM Null-Array Parsing Fix Report

## Plan (EN)
- Problem observed on Stock Recommend page: backend often returned `InvalidOperationException` with message `The requested operation requires an element of type 'Array', but the target element has type 'Null'.`
- Investigated frontend and backend paths; located failing parser logic in `OpenAiProvider` Gemini streaming/non-streaming JSON handling.
- Planned fix: add strict `JsonElement.ValueKind` checks before array operations and add a regression unit test for `parts: null` stream payload.

## 计划（ZH）
- 在股票推荐页复现到后端异常：`The requested operation requires an element of type 'Array', but the target element has type 'Null'.`
- 排查前后端后，定位到 `OpenAiProvider` 对 Gemini 流式/非流式返回 JSON 的解析逻辑。
- 修复方案：在数组操作前增加 `JsonElement.ValueKind` 校验，并补充 `parts: null` 的回归单测。

## Development (EN)
- Updated `backend/SimplerJiangAiAgent.Api/Infrastructure/Llm/OpenAiProvider.cs`:
  - Hardened `ChatWithGeminiInternetAsync` parsing: guard `candidates` and `parts` with `ValueKind == Array` checks.
  - Hardened `ExtractGeminiChunks` parsing: guard candidate/content/parts node types and only read string text parts.
- Updated `backend/SimplerJiangAiAgent.Api.Tests/OpenAiProviderTests.cs`:
  - Added regression test `StreamChatAsync_IgnoresNullPartsPayload_WithoutThrowing`.
  - Simulated SSE response with `parts: null` and verified stream returns empty chunks without throwing.
- Updated rule base:
  - Added bilingual actionable rule to `.github/copilot-instructions.md` for JSON `ValueKind` validation before array APIs.

## 开发（ZH）
- 修改 `backend/SimplerJiangAiAgent.Api/Infrastructure/Llm/OpenAiProvider.cs`：
  - 在 `ChatWithGeminiInternetAsync` 中对 `candidates`、`parts` 增加 `ValueKind == Array` 防护。
  - 在 `ExtractGeminiChunks` 中增加候选节点/内容节点/parts 节点类型校验，仅提取字符串类型 `text`。
- 修改 `backend/SimplerJiangAiAgent.Api.Tests/OpenAiProviderTests.cs`：
  - 新增回归测试 `StreamChatAsync_IgnoresNullPartsPayload_WithoutThrowing`。
  - 模拟 `parts: null` 的 SSE 返回，验证不会抛异常且返回空分片。
- 规则补充：
  - 在 `.github/copilot-instructions.md` 增加中英文规则，要求数组 API 前必须做 `ValueKind` 校验。

## Test Commands and Results (EN + ZH)
1. Command:
   - `dotnet test .\\backend\\SimplerJiangAiAgent.Api.Tests\\SimplerJiangAiAgent.Api.Tests.csproj --filter "OpenAiProviderTests"`
2. First run result:
   - Failed due to file lock (`SimplerJiangAiAgent.Api.exe` in use by PID 16820).
   - 首次失败：后端进程占用可执行文件导致构建锁定。
3. Unblock command:
   - `Stop-Process -Id 16820 -Force`
4. Second run result:
   - Passed: Total 3, Failed 0, Passed 3, Skipped 0.
   - 二次执行通过：总计 3，用例全部通过。

## Issues / Notes
- No frontend code changes were required for this fix.
- This fix reduces intermittent runtime parser failures when upstream Gemini returns nullable arrays in SSE payloads.
- 本次无需改动前端；修复目标是提升后端对上游不稳定 JSON 结构的容错能力。
