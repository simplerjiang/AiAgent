# OPS-006 执行指令（给 OpenCode 执行者）

你是执行者，我是指挥与验收者。请严格按以下步骤执行，不得跳步。

## 目标
完成项目启动与端到端联调验证，输出可复现结果。

## 约束
1. 严格遵守仓库规则：`.github/copilot-instructions.md`、`AGENTS.md`、`.automation/README.md`。
2. 前后端分离任务必须先确认后端可用，再验证前端与 UI。
3. 测试顺序固定：单元测试 -> Edge MCP。
4. 禁止明文输出或写入任何 API Key/Token。
5. 每一步都要记录命令、结果、端口、异常与修复动作。

## 执行步骤
1) 后端可用性
- 启动后端（避免端口冲突，必要时改用空闲端口并记录）
- 健康检查接口必须返回成功

2) 前端与构建
- 执行前端单元测试
- 构建前端产物

3) Edge MCP 验证
- 使用 Edge（msedge）+ 专用 user-data-dir（`.automation/edge-profile`）
- 验证页面可显示、核心交互可点击
- 同步检查后端日志无错误
- 记录 trace/video 输出目录

4) 回归与收尾
- 重新执行必要测试确保修复后仍通过
- 形成双语报告（EN+ZH）写入 `.automation/reports`
- 更新 `.automation/tasks.json` 中 OPS-006 的 `dev/test` 状态

## 输出要求
- 给我一份结构化汇报：
  - 执行命令清单
  - 每步结果（通过/失败）
  - 端口与路径
  - 问题与修复
  - 最终结论
