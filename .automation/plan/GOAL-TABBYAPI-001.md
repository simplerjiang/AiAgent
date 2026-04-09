# GOAL-TABBYAPI-001: ExLlamaV2 + TabbyAPI 本地推理加速集成

## 概述

在保留现有 Ollama 推理引擎的基础上，新增 ExLlamaV2 + TabbyAPI 作为可选的本地推理后端，利用 ExLlamaV2 更高效的 NVIDIA GPU 推理能力提升本地模型速度。

## 背景

| 指标 | 当前 (Ollama/llama.cpp) | 目标 (ExLlamaV2) |
|------|------------------------|------------------|
| 推理速度 | 55-61 tok/s | 80-100+ tok/s |
| 引擎 | llama.cpp | ExLlamaV2 |
| 模型格式 | GGUF (Q4_K_M) | EXL2 |
| GPU | RTX 5060 8GB | 同 |

## 架构设计

### 现有 Provider 体系

```
LlmService
  ├── OpenAiProvider (ProviderType: "openai") ← TabbyAPI 可复用
  ├── OllamaProvider (ProviderType: "ollama")
  └── AntigravityProvider (ProviderType: "antigravity")
```

### TabbyAPI 接入方式

**Phase 1（零代码改动）**：利用现有 `OpenAiProvider`，配置新 Provider：
```json
{
  "Provider": "tabbyapi",
  "ProviderType": "openai",
  "BaseUrl": "http://localhost:5000",
  "Model": "gemma4-e2b-exl2",
  "Enabled": true
}
```

**Phase 2（可选优化）**：如需 TabbyAPI 特有功能（如自定义采样参数、连续批处理控制），创建专用 `TabbyApiProvider`。

### 部署架构

```
┌─────────────────────────────┐
│       应用后端 (ASP.NET)      │
│  LlmService                 │
│    ├─ OllamaProvider ───────┼──→ Ollama (localhost:11434)
│    └─ OpenAiProvider ───────┼──→ TabbyAPI (localhost:5000)
└─────────────────────────────┘
                                    ↓
                              ExLlamaV2 引擎
                                    ↓
                              EXL2 模型文件
```

## 实施计划

### Story 1: 环境验证（预研）
**验收标准**：确认 ExLlamaV2 + TabbyAPI 可在当前硬件上运行  
**状态**：TODO

- [ ] 1.1 检查 ExLlamaV2 对 RTX 5060 (Blackwell/sm_120) 的支持状态
- [ ] 1.2 检查 HuggingFace 上 gemma4 EXL2 量化模型的可用性
- [ ] 1.3 如无现成模型，评估自行转换成本（需要 Python + exllamav2 convert 工具）
- [ ] 1.4 安装 Python 3.10+、PyTorch 2.x、ExLlamaV2
- [ ] 1.5 安装 TabbyAPI，配置并启动
- [ ] 1.6 手动测试 TabbyAPI 的 `/v1/chat/completions` 端点
- [ ] 1.7 运行速度基准测试，与 Ollama 57 tok/s 对比

**阻塞条件**：
- 如果 ExLlamaV2 不支持 RTX 5060 → 等待 ExLlamaV2 更新或放弃
- 如果 gemma4 无 EXL2 模型且转换失败 → 使用其他支持的模型测试

### Story 2: 应用集成
**验收标准**：用户可通过 Admin UI 在 Ollama 和 TabbyAPI 之间切换  
**状态**：TODO  
**依赖**：Story 1 通过

- [ ] 2.1 通过 Admin API 添加 TabbyAPI Provider 配置（ProviderType=openai）
- [ ] 2.2 验证 LlmService 正确路由到 OpenAiProvider
- [ ] 2.3 端到端测试：通过应用发送实际研究任务到 TabbyAPI
- [ ] 2.4 对比 Ollama 和 TabbyAPI 的输出质量
- [ ] 2.5 验证 Provider 切换功能（Active Provider 从 ollama 切到 tabbyapi）
- [ ] 2.6 错误处理验证：TabbyAPI 未启动时的降级行为

### Story 3: 运维与文档
**验收标准**：有完整的启动/维护文档  
**状态**：TODO  
**依赖**：Story 2 通过

- [ ] 3.1 编写 TabbyAPI 启动脚本（Windows .bat 或 .ps1）
- [ ] 3.2 更新 README.md 添加 TabbyAPI 配置说明
- [ ] 3.3 更新 start-all.bat（可选：自动启动 TabbyAPI）
- [ ] 3.4 记录性能对比数据到文档

## 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| ExLlamaV2 不支持 RTX 5060 | 中 | 阻塞 | Story 1 预研阶段即可发现，回退到 Ollama |
| gemma4 无 EXL2 格式 | 中 | 延迟 | 自行转换或使用其他模型（如 Qwen2.5） |
| TabbyAPI 稳定性不足 | 低 | 可控 | 保留 Ollama 作为回退选项 |
| 两套服务维护成本高 | 确定 | 持续 | 日常只启动一个，另一个作备选 |

## 决策记录

- **保留 Ollama**：Ollama 作为默认推理引擎，稳定可靠
- **TabbyAPI 作为可选加速**：仅在需要更快速度时启用
- **Phase 1 先行**：利用现有 OpenAiProvider，零代码改动验证可行性
- **不做强制迁移**：用户可根据需要自由切换

## 任务分级

**L 级**（新功能，涉及外部依赖）  
流程：Dev → Test → UI Designer → User Rep + 写报告

## 附录：当前 Ollama 优化已完成

| 优化项 | 状态 |
|--------|------|
| `OLLAMA_FLASH_ATTENTION=1` | ✅ 已设置 |
| `OLLAMA_KV_CACHE_TYPE=q8_0` | ✅ 已设置 |
| `num_gpu=99` 默认参数 | ✅ 已加入代码 |
| 速度 37.9 → 57.5 tok/s | ✅ 已验证 |
