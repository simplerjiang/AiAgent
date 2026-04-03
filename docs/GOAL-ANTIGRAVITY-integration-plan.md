# GOAL-ANTIGRAVITY: Google Antigravity API 集成方案

**日期:** 2026-04-02  
**状态:** 方案评审中  
**风险等级:** ⚠️ 高（违反 Google ToS，存在封号风险，用户已知悉并接受）

---

## 1. 背景与动机

### 当前痛点
- 系统当前通过第三方中转站（bltcy.ai 等）访问 LLM API
- 中转站网络质量极差，可能本身也是非法获取的 API 通道
- 频繁出现请求超时、连接不稳定等问题，严重影响股票分析功能

### 目标
通过 Google Antigravity 内部 API 网关，直连 Google 基础设施获取 AI 模型访问，作为**扩展方案**补充现有 LLM 接口。

> **重要原则:** 现有的所有 LLM Provider（bltcy.ai 中转、Gemini 官方 API 等）全部保留不动。Antigravity 是新增的可选通道，用户在 LLM 设置中自由选择和切换。系统应监控各通道的可用性状态。

### 参考项目
[opencode-antigravity-auth-updated](https://github.com/insign/opencode-antigravity-auth-updated) — 一个 TypeScript/Node.js 的 OpenCode 插件，已验证可通过 Antigravity API 访问 Claude 和 Gemini 模型。

---

## 2. 技术原理

### Antigravity 是什么
Google Antigravity 是 Google 的 IDE AI 助手产品（类似 GitHub Copilot），其后端通过 **Cloud Code Assist** 统一网关 API 访问多种 AI 模型。该网关使用 Gemini 格式的请求/响应，但可以路由到 Claude、Gemini、GPT-OSS 等多种模型后端。

### 认证流程

```
用户浏览器 → Google OAuth 登录（使用 Antigravity 的 Client ID）
    ↓
获得 authorization_code
    ↓
后端用 code 换取 refresh_token + access_token
    ↓
access_token 有效期 ~1小时，过期后用 refresh_token 自动刷新
    ↓
用 access_token 调用 Antigravity API
```

### API 调用流程

```
C# 后端 → 构造 Gemini 格式请求体
    ↓
POST https://daily-cloudcode-pa.sandbox.googleapis.com/v1internal:generateContent
    ↓
Headers: Authorization: Bearer {access_token}
         User-Agent: antigravity/1.19.4 windows/amd64
         X-Goog-Api-Client: google-cloud-sdk vscode_cloudshelleditor/0.1
         Client-Metadata: {"ideType":"ANTIGRAVITY","platform":"WINDOWS","pluginType":"GEMINI"}
    ↓
Antigravity 网关内部路由到对应模型后端
    ↓
返回 Gemini 格式响应 → 解析提取内容
```

---

## 3. 关键技术参数

### OAuth 参数（来自 Antigravity IDE）

| 参数 | 值 |
|------|-----|
| Client ID | `<REDACTED>` |
| Client Secret | `<REDACTED>` |
| Authorization URL | `https://accounts.google.com/o/oauth2/v2/auth` |
| Token URL | `https://oauth2.googleapis.com/token` |
| Redirect URI | `http://localhost:{port}/oauth-callback`（端口可自定义） |
| Scopes | `cloud-platform`, `userinfo.email`, `userinfo.profile`, `cclog`, `experimentsandconfigs` |

### API 端点（按优先级降级）

| 端点 | 用途 |
|------|------|
| `https://daily-cloudcode-pa.sandbox.googleapis.com` | **主端点**（Daily Sandbox） |
| `https://autopush-cloudcode-pa.sandbox.googleapis.com` | 备用端点 1 |
| `https://cloudcode-pa.googleapis.com` | 备用端点 2（Production） |

### API 路径

| 操作 | 路径 |
|------|------|
| 非流式生成 | `/v1internal:generateContent` |
| 流式生成（SSE） | `/v1internal:streamGenerateContent?alt=sse` |
| 项目发现 | `/v1internal:loadCodeAssist` |

### 可用模型

| 模型 ID | 类型 | 说明 |
|---------|------|------|
| `claude-opus-4-6-thinking` | Anthropic | Claude Opus 4.6 + 思考链 |
| `claude-sonnet-4-6` | Anthropic | Claude Sonnet 4.6 |
| `gemini-3-pro-high` | Google | Gemini 3 Pro（高思考预算） |
| `gemini-3-pro-low` | Google | Gemini 3 Pro（低思考预算） |
| `gemini-3-flash` | Google | Gemini 3 Flash |
| `gemini-3.1-pro` | Google | Gemini 3.1 Pro |
| `gpt-oss-120b-medium` | Other | GPT-OSS 120B |

### 请求体格式（Gemini 统一格式）

```json
{
  "project": "{project_id}",
  "model": "{model_id}",
  "request": {
    "contents": [
      {
        "role": "user",
        "parts": [{ "text": "用户消息" }]
      }
    ],
    "generationConfig": {
      "maxOutputTokens": 8192,
      "temperature": 0.7
    },
    "systemInstruction": {
      "parts": [{ "text": "系统提示词" }]
    }
  },
  "userAgent": "antigravity",
  "requestId": "{uuid}"
}
```

### 响应体格式

```json
{
  "candidates": [
    {
      "content": {
        "parts": [
          { "text": "模型回复内容" }
        ],
        "role": "model"
      },
      "finishReason": "STOP"
    }
  ],
  "usageMetadata": {
    "promptTokenCount": 100,
    "candidatesTokenCount": 200
  }
}
```

---

## 4. 实施方案

### 4.1 新增 `AntigravityProvider`（核心）

**文件:** `backend/SimplerJiangAiAgent.Api/Infrastructure/Llm/AntigravityProvider.cs`

**职责:**
- 实现 `ILlmProvider` 接口，`Name = "antigravity"`
- OAuth access_token 自动刷新管理
- 请求构造：将 `LlmChatRequest` 转换为 Antigravity Gemini 格式
- 响应解析：从 `candidates[].content.parts[].text` 提取内容
- Endpoint 降级：主端点失败后自动切换备用端点
- ProjectId 自动获取和缓存

**LlmProviderSettings 复用策略:**

| 字段 | 用途 |
|------|------|
| `Provider` | e.g. `"antigravity_main"` |
| `ProviderType` | `"antigravity"` |
| `ApiKey` | 存储 `refresh_token` |
| `BaseUrl` | 留空（使用内置端点）或自定义端点 |
| `Model` | 模型名，如 `"gemini-3-pro-high"`, `"claude-opus-4-6-thinking"` |
| `Project` | 存储 `projectId`（自动获取后缓存） |

**核心逻辑伪代码:**

```csharp
public sealed class AntigravityProvider : ILlmProvider
{
    public string Name => "antigravity";
    
    // Token 缓存（内存级）
    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiry;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    public async Task<LlmChatResult> ChatAsync(
        LlmProviderSettings settings, 
        LlmChatRequest request, 
        CancellationToken ct)
    {
        // 1. 确保 access_token 有效
        var accessToken = await EnsureAccessTokenAsync(settings, ct);
        
        // 2. 确保有 projectId
        var projectId = await EnsureProjectIdAsync(settings, accessToken, ct);
        
        // 3. 构造请求
        var model = request.Model ?? settings.Model ?? "gemini-3-pro-high";
        var body = BuildRequestBody(projectId, model, request, settings);
        
        // 4. 发送请求（带端点降级）
        var response = await SendWithFallbackAsync(accessToken, body, ct);
        
        // 5. 解析响应
        return ParseResponse(response);
    }
}
```

### 4.2 OAuth 登录端点

**新增端点:**

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/admin/antigravity/auth-start` | 生成 OAuth 授权 URL，启动本地回调监听（随机可用端口） |
| GET | `http://localhost:{random}/antigravity-callback` | OAuth 回调（临时 listener，非后端路由） |
| GET | `/api/admin/antigravity/auth-status` | 检查当前 Antigravity 登录状态和可用性 |

**OAuth 流程:**

```
1. 前端调用 POST /api/admin/antigravity/auth-start
   → 后端生成 PKCE challenge + authorization URL
   → 启动临时 HTTP listener 等待回调
   → 返回 { authUrl: "https://accounts.google.com/..." }

2. 前端打开新窗口/标签页访问 authUrl
   → 用户在 Google 页面登录授权

3. Google 重定向到 http://localhost:{random_port}/antigravity-callback?code=xxx&state=yyy
   → 后端临时 listener（随机可用端口）接收 code
   → 用 code + PKCE verifier 换取 refresh_token + access_token
   → 自动存储到 llm-settings.local.json（ApiKey 字段）
   → 自动调用 loadCodeAssist 获取 projectId
   → 存储 projectId 到 settings.Project

4. 前端轮询 GET /api/admin/antigravity/auth-status 确认登录成功
```

**安全注意:**
- 回调 listener 仅监听 localhost
- 使用 PKCE 防止 CSRF
- refresh_token 仅存储在 `llm-settings.local.json`（.gitignore 排除）
- 回调完成后立即关闭临时 listener

### 4.3 依赖注册

**修改文件:** `backend/SimplerJiangAiAgent.Api/Modules/Llm/LlmModule.cs`

```csharp
// 新增注册
services.AddHttpClient<AntigravityProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
services.AddSingleton<ILlmProvider, AntigravityProvider>();
```

### 4.4 前端适配（最小改动）

**修改文件:** 前端 LLM 设置页面

- ProviderType 下拉增加 `"antigravity"` 选项
- 当 ProviderType 为 antigravity 时：
  - ApiKey 输入框标签改为 "Refresh Token"
  - 新增 "Google 账号登录" 按钮，触发 OAuth 流程
  - Model 下拉提供 Antigravity 支持的模型列表
  - BaseUrl 字段隐藏（使用内置端点）

---

## 5. 请求头伪装策略

Antigravity API 通过 User-Agent 和 Client-Metadata 识别客户端身份。需要模拟合法的 IDE 客户端：

```
User-Agent: antigravity/1.19.4 windows/amd64
X-Goog-Api-Client: google-cloud-sdk vscode_cloudshelleditor/0.1
Client-Metadata: {"ideType":"ANTIGRAVITY","platform":"WINDOWS","pluginType":"GEMINI"}
Content-Type: application/json
```

**版本号维护:** Antigravity 版本号可能会随着 Google 更新而变化。如果出现 "This version is no longer supported" 错误，需要更新 User-Agent 中的版本号。建议将版本号设为可通过配置文件修改，无需重新编译。

---

## 6. 错误处理与降级策略

### Token 刷新失败
| 错误 | 处理 |
|------|------|
| `invalid_grant` | refresh_token 已失效/被撤销，需要用户重新登录 |
| 网络错误 | 重试 1 次，仍失败则抛出明确异常 |
| 其他 4xx | 记录错误，抛出异常 |

### API 请求失败
| 错误 | 处理 |
|------|------|
| 401 Unauthorized | 强制刷新 access_token 后重试 1 次 |
| 429 Rate Limited | 记录日志，返回错误给调用方（不自动重试，避免加剧限速） |
| 5xx Server Error | 切换到下一个备用端点重试 |
| 网络超时 | 切换到下一个备用端点重试 |
| "version no longer supported" | 记录日志，提示更新 User-Agent 版本号 |

### 与现有 Provider 的降级关系
- AntigravityProvider 作为独立的 ProviderType 注册
- 用户可在 LLM 设置中配置多个 Provider，手动切换 active provider
- **未来可选:** 实现自动 failover，当 Antigravity 失败时自动切换到备用 Provider

---

## 7. 文件变更清单

### 新增文件
| 文件 | 说明 |
|------|------|
| `backend/.../Infrastructure/Llm/AntigravityProvider.cs` | Antigravity API 调用实现 |
| `backend/.../Infrastructure/Llm/AntigravityOAuthService.cs` | OAuth 登录流程管理（PKCE、token 交换、回调监听） |
| `backend/.../Infrastructure/Llm/AntigravityConstants.cs` | 端点、Client ID、Headers 等常量 |
| `backend/.../Tests/AntigravityProviderTests.cs` | 单元测试 |

### 修改文件
| 文件 | 修改内容 |
|------|----------|
| `backend/.../Modules/Llm/LlmModule.cs` | 注册 AntigravityProvider + OAuth 端点 |
| `frontend/src/modules/llm/...` | 设置页面增加 Antigravity 选项和 OAuth 登录按钮 |
| `backend/.../Infrastructure/Llm/JsonFileLlmSettingsStore.cs` | `NormalizeProviderType` 增加 `"antigravity"` 映射 |

---

## 8. 风险与缓解

### 8.1 账号封禁风险（高）
- **风险:** Google 可能检测到非正常 IDE 使用模式，封禁 Google 账号
- **缓解:** 
  - 使用非主力 Google 账号
  - 控制请求频率，避免异常高频调用
  - 在 UI 上加明确警告提示

### 8.2 API 版本变更（中）
- **风险:** Google 可能随时更新 Antigravity API 格式、端点或认证方式
- **缓解:**
  - User-Agent 版本号可配置
  - 端点降级机制
  - 错误日志中包含详细的原始响应用于诊断

### 8.3 ProjectId 获取失败（低）
- **风险:** `loadCodeAssist` 可能无法返回 projectId
- **缓解:** 使用默认 projectId `"rising-fact-p41fc"`（该项目在参考实现中验证可用）

### 8.4 与用户 Google One 会员的关系
- Google One 会员身份可能提供额外的配额或优先级
- 但 Antigravity 配额与 Google One 是独立体系
- Google One 的主要作用是确保账号不太容易因为低使用量被标记为异常

---

## 9. 实施阶段

### Phase 1: MVP（后端核心）≈ 主要工作量
1. 新增 `AntigravityConstants.cs`
2. 新增 `AntigravityOAuthService.cs`（OAuth 流程，随机端口回调）
3. 新增 `AntigravityProvider.cs`（API 调用）
4. 注册到 DI 和路由
5. 单元测试
6. 手动测试：通过 API 端点触发 OAuth 登录 → 用 `gemini-3-flash` 发送测试请求验证

> **优先测试模型:** `gemini-3-flash`（速度快，适合验证可用性）

### Phase 2: 前端集成
1. LLM 设置页面增加 Antigravity 选项
2. OAuth 登录按钮和流程
3. 模型选择器
4. 登录状态提示

### Phase 3: 增强（可选）
1. 多账号支持和轮换
2. Provider 间自动 failover
3. 请求频率控制
4. 使用量统计和展示

---

## 10. 多通道共存策略

本方案的核心定位是**扩展**，不是替换。所有现有 Provider 保持不变。

| 通道 | 类型 | 定位 |
|------|------|------|
| bltcy.ai 中转 | 现有 `default` | 保留，作为已有通道 |
| Gemini 官方 API | 现有 `gemini_official` | 保留，合规的官方通道 |
| **Antigravity API** | **新增** `antigravity` | 扩展通道，免费访问 Claude+Gemini |

用户可以在 LLM 设置界面中：
- 配置多个 Provider（包括 Antigravity）
- 查看各通道的**可用性状态**
- 一键切换 active provider
- 未来可选：配置自动 failover 优先级

---

## 附录 A: 现有代码架构兼容性分析

当前系统的 LLM 抽象层设计良好：
- `ILlmProvider` 接口简洁（`Name` + `ChatAsync`）
- `LlmProviderSettings` 字段足够灵活（`ProviderType`, `ApiKey`, `BaseUrl`, `Model`, `Project`）
- `LlmService` 通过 `ProviderType` 路由到具体 Provider
- `JsonFileLlmSettingsStore` 已支持多 Provider 和安全的本地密钥存储

新增 `AntigravityProvider` 完全符合现有架构，**无需修改现有接口或数据模型**。

## 附录 B: 请求频率建议

基于参考项目的实践经验：
- Claude 模型：建议间隔 ≥ 5 秒/请求
- Gemini 模型：建议间隔 ≥ 2 秒/请求
- 遇到 429 后：指数退避，首次 5 秒，最长 60 秒
- 日总请求量：建议控制在 500 次以内，避免触发异常检测
