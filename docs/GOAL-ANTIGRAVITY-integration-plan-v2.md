# GOAL-ANTIGRAVITY: Google Antigravity API 集成详细方案

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
[opencode-antigravity-auth-updated](https://github.com/insign/opencode-antigravity-auth-updated)（TypeScript/Node.js）已验证可通过 Antigravity API 访问 Claude 和 Gemini 模型。本文档提取了该项目的**全部关键实现细节**，实施时无需再查看原项目代码。

---

## 2. 技术原理

### 什么是 Antigravity
Google Antigravity 是 Google 的 IDE AI 助手产品（类似 GitHub Copilot），其后端通过 **Cloud Code Assist** 统一网关 API 访问多种 AI 模型。该网关使用 Gemini 格式的请求/响应，但可以路由到 Claude、Gemini、GPT-OSS 等多种模型后端。

### 整体架构

```
C# 后端 AntigravityProvider
    ↓
构造 Gemini 格式请求体 + Antigravity Headers
    ↓
POST https://daily-cloudcode-pa.sandbox.googleapis.com/v1internal:generateContent
    ↓ (如果失败则降级到备用端点)
Antigravity 网关（Google 内部）
    ↓ (根据 model 字段路由)
Claude/Gemini/GPT-OSS 模型后端
    ↓
返回 Gemini 格式响应（candidates[].content.parts[].text）
    ↓
C# 后端解析提取内容 → 返回 LlmChatResult
```

---

## 3. OAuth 认证 — 完整流程

### 3.1 认证总览

```
首次登录:
  用户浏览器 → Google OAuth 登录（使用 Antigravity 的 Client ID + PKCE）
      → 获得 authorization_code
      → 后端用 code 换取 refresh_token + access_token
      → 存储 refresh_token → 获取 projectId
      → 登录完成

后续请求:
  后端用 refresh_token → https://oauth2.googleapis.com/token → 新 access_token（~1小时有效）
      → 用 access_token 调用 Antigravity API
```

### 3.2 OAuth 常量

```csharp
// ======== 核心 OAuth 参数 ========
const string ClientId = "<REDACTED>";
const string ClientSecret = "<REDACTED>";
const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
const string TokenUrl = "https://oauth2.googleapis.com/token";
const string UserInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";

// ======== Scopes ========
string[] Scopes = new[]
{
    "https://www.googleapis.com/auth/cloud-platform",
    "https://www.googleapis.com/auth/userinfo.email",
    "https://www.googleapis.com/auth/userinfo.profile",
    "https://www.googleapis.com/auth/cclog",
    "https://www.googleapis.com/auth/experimentsandconfigs"
};

// ======== Redirect URI（使用随机端口）========
// 格式: http://localhost:{random_port}/antigravity-callback
// 端口在启动 OAuth 监听时动态分配
```

### 3.3 Step 1: 生成授权 URL（带 PKCE）

```
1. 生成 PKCE:
   - code_verifier: 随机 43-128 字符的 Base64URL 字符串
   - code_challenge: SHA256(code_verifier) 的 Base64URL 编码

2. 生成 state 参数:
   - 将 { verifier, projectId } JSON → Base64URL 编码
   - 用于 CSRF 防护和 PKCE verifier 回传

3. 构造 URL:
   https://accounts.google.com/o/oauth2/v2/auth?
     client_id={ClientId}
     &response_type=code
     &redirect_uri=http://localhost:{port}/antigravity-callback
     &scope={Scopes joined by space}
     &code_challenge={challenge}
     &code_challenge_method=S256
     &state={base64url encoded state}
     &access_type=offline        ← 关键：请求 refresh_token
     &prompt=consent             ← 关键：强制同意页面，确保返回 refresh_token
```

### 3.4 Step 2: 启动本地回调监听器

```
1. 在随机可用端口上启动临时 HTTP 服务器
2. 仅监听 localhost（127.0.0.1）
3. 监听路径: /antigravity-callback
4. 超时时间: 5 分钟
5. 收到回调后:
   - 解析 URL 参数: code 和 state
   - 返回 HTML 成功页面给浏览器
   - 立即关闭监听器
6. 将 code 和 state 传递给 Token 交换逻辑
```

### 3.5 Step 3: 用 Code 交换 Token

```http
POST https://oauth2.googleapis.com/token
Content-Type: application/x-www-form-urlencoded;charset=UTF-8
Accept: */*
User-Agent: google-api-nodejs-client/9.15.1

client_id={ClientId}
&client_secret={ClientSecret}
&code={authorization_code}
&grant_type=authorization_code
&redirect_uri=http://localhost:{port}/antigravity-callback
&code_verifier={从 state 参数中解码出的 verifier}
```

**成功响应:**
```json
{
  "access_token": "ya29.a0AZ...",
  "expires_in": 3599,
  "refresh_token": "1//0d...",
  "scope": "...",
  "token_type": "Bearer"
}
```

> **关键:** `refresh_token` 只在首次授权时返回（因为设置了 `prompt=consent`）。必须安全保存。

### 3.6 Step 4: 获取用户信息（可选）

```http
GET https://www.googleapis.com/oauth2/v1/userinfo?alt=json
Authorization: Bearer {access_token}
User-Agent: google-api-nodejs-client/9.15.1
```

**响应:**
```json
{
  "email": "user@gmail.com",
  "name": "User Name",
  "picture": "https://..."
}
```

### 3.7 Step 5: 获取 Project ID

登录完成后，需要获取 Antigravity 的 `projectId`，这是每个 API 请求的必填字段。

```http
POST https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist
Content-Type: application/json
Authorization: Bearer {access_token}
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Antigravity/1.19.4 Chrome/138.0.7204.235 Electron/37.3.1 Safari/537.36
Client-Metadata: {"ideType":"ANTIGRAVITY","pluginType":"GEMINI"}

{
  "metadata": {
    "ideType": "ANTIGRAVITY",
    "pluginType": "GEMINI"
  }
}
```

> **⚠️ 重要（实战验证）:** `platform` 字段必须省略。所有字符串枚举值（`WINDOWS`/`LINUX`/`DARWIN`/`MACOS`）均返回 400。见附录 A。

**成功响应:**
```json
{
  "cloudaicompanionProject": "some-project-id-abc",
  "currentTier": { "id": "default-tier" },
  "allowedTiers": [
    { "id": "default-tier", "isDefault": true }
  ]
}
```

**Project ID 提取逻辑:**
```
1. 如果 cloudaicompanionProject 是字符串 → 直接使用
2. 如果 cloudaicompanionProject 是对象 → 使用 .id 字段
3. 如果都没有 → 使用默认值 "rising-fact-p41fc"
```

**端点尝试顺序（Project ID 获取专用）:**
1. `https://cloudcode-pa.googleapis.com`（prod 优先）
2. `https://daily-cloudcode-pa.sandbox.googleapis.com`
3. `https://autopush-cloudcode-pa.sandbox.googleapis.com`

### 3.8 Token 刷新流程

Access token 有效期约 1 小时。过期后用 refresh_token 自动刷新：

```http
POST https://oauth2.googleapis.com/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&refresh_token={stored_refresh_token}
&client_id={ClientId}
&client_secret={ClientSecret}
```

**成功响应:**
```json
{
  "access_token": "ya29.new...",
  "expires_in": 3599,
  "token_type": "Bearer"
}
```

> **注意:** 刷新响应可能包含新的 `refresh_token`。如果包含，必须更新存储的值。

**Token 过期判断:**
```csharp
// 提前 60 秒视为过期，避免边界情况
bool IsExpired(DateTimeOffset expiry)
{
    return expiry <= DateTimeOffset.UtcNow.AddSeconds(60);
}

// 计算过期时间
DateTimeOffset CalculateExpiry(DateTimeOffset requestTime, int expiresInSeconds)
{
    if (expiresInSeconds <= 0) return requestTime; // 立即过期（异常情况）
    return requestTime.AddSeconds(expiresInSeconds);
}
```

**刷新失败处理:**

| 错误码 | 含义 | 处理 |
|--------|------|------|
| `invalid_grant` | refresh_token 已被 Google 撤销 | 清除缓存的 token，提示用户重新登录 |
| 400/401 其他 | 请求格式错误或认证问题 | 记录错误详情，抛出异常 |
| 网络错误 | 连接超时等 | 重试一次，仍失败则抛出 |

---

## 4. API 调用 — 完整规范

### 4.1 API 端点

| 端点 | 优先级 | 说明 |
|------|--------|------|
| `https://daily-cloudcode-pa.sandbox.googleapis.com` | **主端点** | Daily Sandbox |
| `https://autopush-cloudcode-pa.sandbox.googleapis.com` | 备用 1 | Autopush Sandbox |
| `https://cloudcode-pa.googleapis.com` | 备用 2 | Production |

**API 路径:**
- 非流式: `/v1internal:generateContent`
- 流式 SSE: `/v1internal:streamGenerateContent?alt=sse`

### 4.2 请求头（关键⚠️）

Antigravity 通过 User-Agent 和 Client-Metadata 识别客户端。必须模拟 Antigravity IDE：

```http
Authorization: Bearer {access_token}
Content-Type: application/json
User-Agent: antigravity/1.19.4 windows/amd64
```

> **重要:** Antigravity 模式下，**只发 User-Agent**，**不发** `X-Goog-Api-Client` 和 `Client-Metadata` 作为 header。`ideType=ANTIGRAVITY` 信息通过请求体传递。

**User-Agent 格式:**
```
antigravity/{version} {platform}/{arch}
```

**可选的 platform/arch 组合:**
- `windows/amd64`
- `darwin/arm64`
- `darwin/amd64`

**版本号获取:** 应用启动时自动获取最新版本号：
1. 首选：`GET https://antigravity-auto-updater-974169037036.us-central1.run.app`（返回纯文本版本号）
2. 备选：从 `https://antigravity.google/changelog` 页面前 5000 字符中正则提取 `\d+\.\d+\.\d+`
3. 兜底：硬编码 `1.19.4`

> **⚠️ 版本号过旧时 Google 会返回 "This version of Antigravity is no longer supported" 错误。** 建议将版本号做成可配置项、启动时自动更新。

**流式请求额外 header:**
```http
Accept: text/event-stream
```

### 4.3 请求体格式（非流式 + 流式通用）

**⚠️ 所有模型统一使用 Gemini 格式！不支持 Anthropic 的 messages 格式。**

```json
{
  "project": "{project_id}",
  "model": "{model_id}",
  "request": {
    "contents": [
      {
        "role": "user",
        "parts": [{ "text": "用户消息内容" }]
      }
    ],
    "systemInstruction": {
      "parts": [{ "text": "系统提示词" }]
    },
    "generationConfig": {
      "maxOutputTokens": 8192,
      "temperature": 0.7
    }
  },
  "requestType": "agent",
  "userAgent": "antigravity",
  "requestId": "agent-{uuid}"
}
```

**请求体各字段说明:**

| 字段 | 必需 | 说明 |
|------|------|------|
| `project` | ✅ | 通过 `loadCodeAssist` 获取的项目 ID，或默认值 `"rising-fact-p41fc"` |
| `model` | ✅ | 模型 ID，如 `"gemini-3-flash"`, `"claude-sonnet-4-6"` |
| `request.contents` | ✅ | Gemini 格式的消息数组 |
| `request.systemInstruction` | ❌ | 系统提示词，**必须是对象**（不能是纯字符串） |
| `request.generationConfig` | ❌ | 生成配置 |
| `requestType` | ❌ | 设为 `"agent"` |
| `userAgent` | ❌ | 设为 `"antigravity"` |
| `requestId` | ❌ | 唯一请求 ID，格式 `"agent-{uuid}"` |

**contents 格式规则:**
- `role` 只能是 `"user"` 或 `"model"`（不是 `"assistant"`！）
- 每条消息的 `parts` 是数组，包含 `{ "text": "..." }` 对象

**systemInstruction 格式（⚠️ 必须是对象）:**
```json
// ✅ 正确
{ "systemInstruction": { "parts": [{ "text": "你是助手" }] } }

// ❌ 错误 — 会返回 400
{ "systemInstruction": "你是助手" }
```

### 4.4 可用模型

| 模型 ID | 类型 | 说明 |
|---------|------|------|
| `gemini-3-flash` | Google | Gemini 3 Flash，速度快 |
| `gemini-3-pro-high` | Google | Gemini 3 Pro，高思考预算 |
| `gemini-3-pro-low` | Google | Gemini 3 Pro，低思考预算 |
| `gemini-3.1-pro` | Google | Gemini 3.1 Pro（取决于 rollout） |
| `claude-sonnet-4-6` | Anthropic | Claude Sonnet 4.6（非思考） |
| `claude-opus-4-6-thinking` | Anthropic | Claude Opus 4.6 + 思考链 |
| `gpt-oss-120b-medium` | Other | GPT-OSS 120B |

### 4.5 非流式响应格式

```json
{
  "response": {
    "candidates": [
      {
        "content": {
          "role": "model",
          "parts": [
            { "text": "模型回复的文本内容" }
          ]
        },
        "finishReason": "STOP"
      }
    ],
    "usageMetadata": {
      "promptTokenCount": 16,
      "candidatesTokenCount": 200,
      "totalTokenCount": 216
    },
    "modelVersion": "claude-sonnet-4-6",
    "responseId": "msg_vrtx_..."
  },
  "traceId": "abc123..."
}
```

**响应解析逻辑（关键⚠️）:**

```csharp
// 1. 最外层有 "response" 包装
var responseWrapper = JsonDocument.Parse(responseText);
var responseObj = responseWrapper.RootElement.GetProperty("response");

// 2. 从 candidates 数组中提取文本
var candidates = responseObj.GetProperty("candidates");
if (candidates.GetArrayLength() == 0) return "";

var content = candidates[0].GetProperty("content");
var parts = content.GetProperty("parts");

// 3. 拼接所有 text parts（可能有多个）
var sb = new StringBuilder();
foreach (var part in parts.EnumerateArray())
{
    // 跳过 thinking blocks
    if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
        continue;
    
    if (part.TryGetProperty("text", out var text))
        sb.Append(text.GetString());
}
return sb.ToString().Trim();
```

> **注意:** 响应最外层有 `"response"` 包装，内部才是标准 Gemini 格式。非流式和流式都有这个 envelope。

### 4.6 流式 SSE 响应格式

Content-Type: `text/event-stream`

每行格式: `data: {json}\n\n`

```
data: {"response":{"candidates":[{"content":{"role":"model","parts":[{"text":"Hello"}]}}]},"traceId":"..."}

data: {"response":{"candidates":[{"content":{"role":"model","parts":[{"text":" world"}]},"finishReason":"STOP"}],"usageMetadata":{...}},"traceId":"..."}

```

**解析流式响应:**
1. 按行读取，找到以 `data: ` 开头的行
2. 去掉 `data: ` 前缀
3. 解析 JSON
4. 提取 `response.candidates[0].content.parts[0].text`
5. 拼接所有 text 片段

### 4.7 错误响应格式

```json
{
  "error": {
    "code": 429,
    "message": "You have exhausted your capacity on this model. Your quota will reset after 3s.",
    "status": "RESOURCE_EXHAUSTED",
    "details": [
      {
        "@type": "type.googleapis.com/google.rpc.RetryInfo",
        "retryDelay": "3.957525076s"
      }
    ]
  }
}
```

**常见错误码:**

| HTTP Code | Status | 说明 | 处理 |
|-----------|--------|------|------|
| 400 | `INVALID_ARGUMENT` | 请求格式错误 | 检查请求体格式 |
| 401 | `UNAUTHENTICATED` | Token 无效/过期 | 强制刷新 token 后重试一次 |
| 403 | `PERMISSION_DENIED` | 无访问权限 | 可能需要重新登录 |
| 404 | `NOT_FOUND` | 模型未找到 | 检查模型名称 |
| 429 | `RESOURCE_EXHAUSTED` | 速率限制 | 解析 retryDelay，等待后重试或报错 |
| 500+ | 服务器错误 | 内部错误 | 切换备用端点重试 |

**"version no longer supported"** 错误：User-Agent 版本号过旧，需更新版本号。

---

## 5. LlmProviderSettings 复用策略

### 5.1 字段映射

现有 `LlmProviderSettings` 完全够用，无需新增字段：

| 字段 | 用途 | 示例值 |
|------|------|--------|
| `Provider` | Provider 标识 | `"antigravity"` |
| `ProviderType` | 路由标识 | `"antigravity"` |
| `ApiKey` | **存储 refresh_token** | `"1//0d..."` |
| `BaseUrl` | 留空（使用内置端点）或自定义 | `""` |
| `Model` | 模型名 | `"gemini-3-flash"` |
| `SystemPrompt` | 系统提示词 | 用户自定义 |
| `ForceChinese` | 强制中文 | `true` |
| `Organization` | **存储 Google 邮箱**（显示用） | `"user@gmail.com"` |
| `Project` | **存储 projectId** | `"rising-fact-p41fc"` |
| `Enabled` | 是否启用 | `true` |

### 5.2 存储位置

- refresh_token（即 ApiKey 字段）只存储在 `llm-settings.local.json`（已被 .gitignore 排除）
- 其他非敏感字段存储在 `llm-settings.json`

---

## 6. 实施方案

### 6.1 新增 AntigravityConstants.cs

```csharp
namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public static class AntigravityConstants
{
    public const string ClientId = "<REDACTED>";
    public const string ClientSecret = "<REDACTED>";
    public const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    public const string UserInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";
    
    public static readonly string[] Scopes = new[]
    {
        "https://www.googleapis.com/auth/cloud-platform",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/cclog",
        "https://www.googleapis.com/auth/experimentsandconfigs"
    };
    
    // API 端点（降级顺序）
    public const string EndpointDaily = "https://daily-cloudcode-pa.sandbox.googleapis.com";
    public const string EndpointAutopush = "https://autopush-cloudcode-pa.sandbox.googleapis.com";
    public const string EndpointProd = "https://cloudcode-pa.googleapis.com";
    
    public static readonly string[] Endpoints = new[] { EndpointDaily, EndpointAutopush, EndpointProd };
    public static readonly string[] LoadEndpoints = new[] { EndpointProd, EndpointDaily, EndpointAutopush };
    
    public const string DefaultProjectId = "rising-fact-p41fc";
    public const string FallbackVersion = "1.19.4";
    public const string VersionUrl = "https://antigravity-auto-updater-974169037036.us-central1.run.app";
    
    // 可选模型列表
    public static readonly string[] AvailableModels = new[]
    {
        "gemini-3-flash",
        "gemini-3-pro-high",
        "gemini-3-pro-low",
        "gemini-3.1-pro",
        "claude-sonnet-4-6",
        "claude-opus-4-6-thinking",
        "gpt-oss-120b-medium"
    };
}
```

### 6.2 新增 AntigravityOAuthService.cs

**职责:**
- PKCE 生成（code_verifier + code_challenge）
- 授权 URL 构造
- 临时回调 HTTP listener（随机端口）
- Code → Token 交换
- Token 刷新
- Project ID 获取
- 版本号自动获取

**PKCE 实现:**
```csharp
// code_verifier: 32字节随机数 → Base64URL
var bytes = RandomNumberGenerator.GetBytes(32);
var codeVerifier = Base64UrlEncode(bytes);

// code_challenge: SHA256(code_verifier) → Base64URL
var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
var codeChallenge = Base64UrlEncode(hash);
```

**临时回调服务器:**
```csharp
// 在随机端口上监听以避免端口冲突
var listener = new HttpListener();
listener.Prefixes.Add($"http://127.0.0.1:0/");
// 或者用 TcpListener 找到空闲端口
var tcpListener = new TcpListener(IPAddress.Loopback, 0);
tcpListener.Start();
var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
tcpListener.Stop();

// 然后使用这个 port 启动 HttpListener
listener.Prefixes.Add($"http://127.0.0.1:{port}/");
```

### 6.3 新增 AntigravityProvider.cs

**核心逻辑:**

```csharp
public sealed class AntigravityProvider : ILlmProvider
{
    public string Name => "antigravity";
    
    // 内存缓存 access_token
    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private string? _cachedProjectId;
    private string _antigravityVersion = AntigravityConstants.FallbackVersion;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    public async Task<LlmChatResult> ChatAsync(
        LlmProviderSettings settings, LlmChatRequest request, CancellationToken ct)
    {
        var refreshToken = settings.ApiKey; // refresh_token 存在 ApiKey 字段
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Antigravity refresh token 未配置，请先登录 Google 账号");
        
        // 1. 确保 access_token 有效
        var accessToken = await EnsureAccessTokenAsync(refreshToken, ct);
        
        // 2. 确保有 projectId
        var projectId = _cachedProjectId ?? settings.Project ?? await FetchProjectIdAsync(accessToken, ct);
        if (string.IsNullOrWhiteSpace(projectId))
            projectId = AntigravityConstants.DefaultProjectId;
        
        // 3. 构造请求体
        var model = request.Model ?? settings.Model ?? "gemini-3-flash";
        var body = BuildRequestBody(projectId, model, request, settings);
        
        // 4. 发送请求（带端点降级和 token 刷新重试）
        var (responseText, statusCode) = await SendWithFallbackAsync(
            accessToken, refreshToken, body, streaming: false, ct);
        
        // 5. 解析响应
        return ParseNonStreamingResponse(responseText);
    }
    
    private string BuildRequestBody(string projectId, string model, 
        LlmChatRequest request, LlmProviderSettings settings)
    {
        var systemPrompt = OpenAiProvider.BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        
        var requestObj = new Dictionary<string, object>
        {
            ["contents"] = new[]
            {
                new { role = "user", parts = new[] { new { text = request.Prompt } } }
            },
            ["generationConfig"] = new
            {
                maxOutputTokens = 8192,
                temperature = request.Temperature ?? 0.7
            }
        };
        
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestObj["systemInstruction"] = new 
            { 
                parts = new[] { new { text = systemPrompt } } 
            };
        }
        
        var wrapper = new
        {
            project = projectId,
            model = model,
            request = requestObj,
            requestType = "agent",
            userAgent = "antigravity",
            requestId = $"agent-{Guid.NewGuid()}"
        };
        
        return JsonSerializer.Serialize(wrapper);
    }
    
    private HttpRequestMessage BuildHttpRequest(string accessToken, string body, 
        string endpoint, bool streaming)
    {
        var path = streaming 
            ? "/v1internal:streamGenerateContent?alt=sse" 
            : "/v1internal:generateContent";
        var url = $"{endpoint}{path}";
        
        var message = new HttpRequestMessage(HttpMethod.Post, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        // ⚠️ 关键：模拟 Antigravity IDE 的 User-Agent
        message.Headers.TryAddWithoutValidation(
            "User-Agent", 
            $"antigravity/{_antigravityVersion} windows/amd64");
        
        // Antigravity 模式下不发 X-Goog-Api-Client 和 Client-Metadata header
        
        message.Content = new StringContent(body, Encoding.UTF8, "application/json");
        
        if (streaming)
        {
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        }
        
        return message;
    }
}
```

### 6.4 依赖注册修改

**修改 LlmModule.cs:**

```csharp
// 在 Register 方法中新增:
services.AddSingleton<AntigravityOAuthService>();
services.AddHttpClient<AntigravityProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
services.AddSingleton<ILlmProvider, AntigravityProvider>();

// 在 MapEndpoints 方法的 secureAdminGroup 中新增:
secureAdminGroup.MapPost("/antigravity/auth-start", async (AntigravityOAuthService oauthService) =>
{
    var result = await oauthService.StartAuthFlowAsync();
    return Results.Ok(new { authUrl = result.AuthUrl, port = result.Port });
});

secureAdminGroup.MapGet("/antigravity/auth-status", async (AntigravityOAuthService oauthService) =>
{
    var status = oauthService.GetAuthStatus();
    return Results.Ok(status);
});
```

### 6.5 NormalizeProviderType 映射

**修改 JsonFileLlmSettingsStore.cs:**

在 `NormalizeProviderType` 方法中增加 `"antigravity"` 映射。

---

## 7. 错误处理与降级策略

### 7.1 端点降级

```
Primary: daily-cloudcode-pa.sandbox.googleapis.com
    ↓ 失败（网络错误/5xx）
Fallback 1: autopush-cloudcode-pa.sandbox.googleapis.com
    ↓ 失败
Fallback 2: cloudcode-pa.googleapis.com
    ↓ 全部失败
抛出异常
```

### 7.2 Token 刷新重试

遇到 401 Unauthorized:
1. 锁定 token 刷新
2. 调用 refresh_token 获取新 access_token
3. 用新 token 重试**一次**
4. 仍然 401 → 清除 token，提示用户重新登录

### 7.3 Rate Limit 处理

遇到 429 RESOURCE_EXHAUSTED:
1. 解析响应中的 `retryDelay`（如 `"3.957525076s"`）
2. 如果 delay ≤ 5 秒，等待后重试
3. 如果 delay > 5 秒，直接向调用方返回错误
4. 记录日志用于后续分析

### 7.4 版本号失效

遇到 "This version of Antigravity is no longer supported":
1. 尝试从版本 URL 获取最新版本号
2. 更新内存中的版本号
3. 用新版本号重试请求
4. 如果获取失败，记录错误，提示用户更新配置

---

## 8. 风险与缓解措施

### 8.1 账号封禁风险（高）
- **风险:** Google 检测到非 IDE 使用模式，封禁 Google 账号
- **缓解:**
  - 在 UI 上加明确警告提示
  - 控制请求频率：建议 Claude ≥5s/次，Gemini ≥2s/次
  - 日总请求量控制在 500 次以内
  - 使用非主力 Google 账号
  - 参考项目的指纹随机化策略（randomize User-Agent platform/arch）

### 8.2 API 版本变更（中）
- **风险:** Google 随时可能更新 API 格式、端点或认证方式
- **缓解:**
  - User-Agent 版本号自动获取 + 可配置
  - 端点降级机制
  - 详细的错误日志（包含原始响应）
  - 模块化设计，API 变更时只需修改 Provider

### 8.3 ProjectId 获取失败（低）
- **缓解:** 使用默认值 `"rising-fact-p41fc"`

### 8.4 与 Google One 的关系
- Google One 会员不直接增加 Antigravity 配额（两者是独立体系）
- Google One 的作用：账号状态更稳定，不太容易被标记为异常

---

## 9. 文件变更清单

### 新增文件
| 文件 | 说明 |
|------|------|
| `backend/.../Infrastructure/Llm/AntigravityProvider.cs` | Antigravity API 调用实现 |
| `backend/.../Infrastructure/Llm/AntigravityOAuthService.cs` | OAuth 登录流程（PKCE、token 交换、回调监听、token 刷新、projectId 获取） |
| `backend/.../Infrastructure/Llm/AntigravityConstants.cs` | 端点、Client ID、Headers 等常量 |
| `backend/.../Tests/AntigravityProviderTests.cs` | 单元测试 |

### 修改文件
| 文件 | 修改内容 |
|------|----------|
| `backend/.../Modules/Llm/LlmModule.cs` | 注册 AntigravityProvider + AntigravityOAuthService + OAuth 端点 |
| `frontend/src/modules/llm/...` | 设置页面增加 Antigravity 选项和 OAuth 登录按钮（Phase 2） |
| `backend/.../Infrastructure/Llm/JsonFileLlmSettingsStore.cs` | `NormalizeProviderType` 增加 `"antigravity"` |

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

## 11. 实施阶段

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
4. 登录状态和可用性指示

### Phase 3: 增强（可选）
1. 多账号支持和轮换
2. Provider 间自动 failover
3. 请求频率控制
4. 使用量统计和展示

---

## 附录 A: 现有代码架构兼容性

当前系统的 LLM 抽象层设计良好：
- `ILlmProvider` 接口简洁：`Name` + `ChatAsync`
- `LlmProviderSettings` 字段足够灵活
- `LlmService` 通过 `ProviderType` 路由到具体 Provider
- `JsonFileLlmSettingsStore` 已支持多 Provider 和安全的本地密钥存储

新增 `AntigravityProvider` 完全符合现有架构，**无需修改现有接口或数据模型**。

## 附录 B: 请求频率建议

| 模型类型 | 建议最小间隔 | 日请求量建议 |
|----------|-------------|-------------|
| Claude | ≥ 5 秒/请求 | ≤ 200 次 |
| Gemini | ≥ 2 秒/请求 | ≤ 400 次 |
| 遇到 429 后 | 指数退避：5s → 10s → 20s → 30s → 60s | — |

## 附录 C: C# 中的 Base64URL 编码

标准 .NET 没有内置 Base64URL，需要手动转换：

```csharp
static string Base64UrlEncode(byte[] data)
{
    return Convert.ToBase64String(data)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

static byte[] Base64UrlDecode(string input)
{
    var s = input.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4)
    {
        case 2: s += "=="; break;
        case 3: s += "="; break;
    }
    return Convert.FromBase64String(s);
}
```

## 附录 D: 完整请求/响应示例

### 请求示例（gemini-3-flash）

```http
POST https://daily-cloudcode-pa.sandbox.googleapis.com/v1internal:generateContent HTTP/1.1
Authorization: Bearer ya29.a0AZoH...
Content-Type: application/json
User-Agent: antigravity/1.19.4 windows/amd64

{
  "project": "rising-fact-p41fc",
  "model": "gemini-3-flash",
  "request": {
    "contents": [
      {
        "role": "user",
        "parts": [{ "text": "请分析贵州茅台今天的走势" }]
      }
    ],
    "systemInstruction": {
      "parts": [{ "text": "你是一个专业的股票分析助手。请使用中文回答。" }]
    },
    "generationConfig": {
      "maxOutputTokens": 8192,
      "temperature": 0.7
    }
  },
  "requestType": "agent",
  "userAgent": "antigravity",
  "requestId": "agent-a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### 响应示例

```json
{
  "response": {
    "candidates": [
      {
        "content": {
          "role": "model",
          "parts": [
            {
              "text": "根据今天的市场数据，贵州茅台（600519）的走势如下：\n\n开盘价：1580元\n最高价：1595元\n最低价：1572元\n当前价：1588元\n涨幅：+0.5%\n\n..."
            }
          ]
        },
        "finishReason": "STOP"
      }
    ],
    "usageMetadata": {
      "promptTokenCount": 45,
      "candidatesTokenCount": 180,
      "totalTokenCount": 225
    },
    "modelVersion": "gemini-3-flash",
    "responseId": "ypM9abPqFKWl0-kPvamgqQw"
  },
  "traceId": "abc123def456"
}
```

### Token 刷新请求示例

```http
POST https://oauth2.googleapis.com/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=1%2F%2F0d...&client_id=<REDACTED>&client_secret=<REDACTED>
```

### Project ID 获取请求示例

```http
POST https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist HTTP/1.1
Content-Type: application/json
Authorization: Bearer ya29.a0AZoH...
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Antigravity/1.19.4 Chrome/138.0.7204.235 Electron/37.3.1 Safari/537.36
Client-Metadata: {"ideType":"ANTIGRAVITY","pluginType":"GEMINI"}

{
  "metadata": {
    "ideType": "ANTIGRAVITY",
    "pluginType": "GEMINI"
  }
}
```

> **注意：不包含 `platform` 字段**（见附录 A：实战验证结论）

---

## 附录 A：实战验证结论（2026-04-02）

### 验证环境
- 账号: jiangsimpler@gmail.com（中国用户，Google One 会员）
- 系统: Windows 11，透明代理访问 Google
- 测试脚本: `.noupload/test-antigravity5.mjs`

### 关键发现

#### 1. `loadCodeAssist` platform 字段

**结论：`metadata.platform` 字段的 proto enum 仅接受空值（省略字段），所有字符串枚举值均被拒绝。**

| 尝试值 | HTTP 状态 | 结论 |
|--------|-----------|------|
| `"WINDOWS"` | 400 | ❌ proto enum 无此值 |
| `"LINUX"` | 400 | ❌ proto enum 无此值 |
| `"DARWIN"` | 400 | ❌ proto enum 无此值 |
| `"MACOS"` | 400 | ❌ proto enum 无此值 |
| 省略 `platform` 字段 | 200 | ✅ 请求成功 |

#### 2. `loadCodeAssist` 响应中无 Project ID（中国账号）

当 `platform` 省略后，`loadCodeAssist` 返回 200，但响应格式为：
```json
{
  "allowedTiers": [
    {
      "id": "standard-tier",
      "userDefinedCloudaicompanionProject": true
    }
  ],
  "ineligibleTiers": [
    { "reasonCode": "UNSUPPORTED_LOCATION" }
  ]
}
```
- `cloudaicompanionProject` 字段缺失
- `UNSUPPORTED_LOCATION` 表示中国区域账号无法自动分配项目
- `userDefinedCloudaicompanionProject: true` 表示需要用户自定义 project ID

**结论：中国账号通过 `loadCodeAssist` 永远无法获取自动分配的 project ID，应直接使用硬编码默认值 `rising-fact-p41fc`。**

#### 3. `generateContent` 验证通过

使用 project ID = `rising-fact-p41fc`，`gemini-3-flash` 模型正常响应：
- 端点：`https://daily-cloudcode-pa.sandbox.googleapis.com/v1internal:generateContent`
- 模型版本字段：`gemini-3-flash`
- Token 计量正常（prompt tokens、candidates tokens）
- 中文 system instruction 生效

#### 4. 正确的 Client-Metadata 头（参考 `getAntigravityHeaders()` 实现）

```
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Antigravity/1.19.4 Chrome/138.0.7204.235 Electron/37.3.1 Safari/537.36
Client-Metadata: {"ideType":"ANTIGRAVITY","pluginType":"GEMINI"}
X-Goog-Api-Client: google-cloud-sdk vscode_cloudshelleditor/0.1
```
（已省略 `platform` 字段）

### C# 集成关键修正

1. `loadCodeAssist` 请求体中**必须省略 `platform` 字段**
2. `Client-Metadata` 头中**必须省略 `platform` 字段**
3. 对于中国账号，`loadCodeAssist` 永远不会返回 project ID → **直接硬编码 `rising-fact-p41fc`，可跳过 loadCodeAssist 步骤**
4. `User-Agent` 应使用完整 Electron/Chrome UA，不是简写的 `antigravity/{version} windows/amd64`
5. `generateContent` 验证完整通过，整个链路可行性已确认 ✅

### 最终验证数据

```
OAuth:           OK (jiangsimpler@gmail.com)
Token 交换:      OK (expires_in: 3599s)
Token 刷新:      OK
loadCodeAssist:  200 OK（无 project ID，使用硬编码）
Project ID:      rising-fact-p41fc（硬编码默认值，永久有效）
generateContent: OK - gemini-3-flash 正常响应
整体验证:        PASS ✅
```

> **注意：不包含 `platform` 字段**（见附录 A：实战验证结论）

---

## 附录 A：实战验证结论（2026-04-02）

### 验证环境
- 账号: jiangsimpler@gmail.com（中国用户，Google One 会员）
- 系统: Windows 11，透明代理访问 Google
- 测试脚本: `.noupload/test-antigravity5.mjs`（v5）

### 关键发现

#### 1. `loadCodeAssist` platform 字段

**结论：`metadata.platform` 字段的 proto enum 仅接受空值（省略字段），所有字符串枚举值均被拒绝。**

| 尝试值 | HTTP 状态 | 结论 |
|--------|-----------|------|
| `"WINDOWS"` | 400 | ❌ proto enum 无此值 |
| `"LINUX"` | 400 | ❌ proto enum 无此值 |
| `"DARWIN"` | 400 | ❌ proto enum 无此值 |
| `"MACOS"` | 400 | ❌ proto enum 无此值 |
| 省略 `platform` 字段 | 200 | ✅ 请求成功 |

#### 2. `loadCodeAssist` 响应中无 Project ID（中国账号）

当 `platform` 省略后，`loadCodeAssist` 返回 200，但响应格式为：
```json
{
  "allowedTiers": [
    {
      "id": "standard-tier",
      "userDefinedCloudaicompanionProject": true
    }
  ],
  "ineligibleTiers": [
    { "reasonCode": "UNSUPPORTED_LOCATION" }
  ]
}
```
- `cloudaicompanionProject` 字段缺失
- `UNSUPPORTED_LOCATION` 表示中国区域账号无法自动分配项目
- `userDefinedCloudaicompanionProject: true` 表示需要用户自定义 project ID

**结论：中国账号通过 `loadCodeAssist` 无法获取自动分配的 project ID，应直接使用硬编码默认值 `rising-fact-p41fc`。**

#### 3. `generateContent` 验证通过

使用 project ID = `rising-fact-p41fc`，`gemini-3-flash` 模型正常响应：
- 端点：`https://daily-cloudcode-pa.sandbox.googleapis.com/v1internal:generateContent`
- 模型版本字段：`gemini-3-flash`
- Token 计量正常（prompt tokens、candidates tokens）
- 中文 system instruction 生效

#### 4. Client-Metadata 头（正确格式）

参考 `getAntigravityHeaders()` 实现：
```
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Antigravity/1.19.4 Chrome/138.0.7204.235 Electron/37.3.1 Safari/537.36
Client-Metadata: {"ideType":"ANTIGRAVITY","pluginType":"GEMINI"}
X-Goog-Api-Client: google-cloud-sdk vscode_cloudshelleditor/0.1
```
（已省略 `platform` 字段）

### C# 集成关键修正

1. `loadCodeAssist` 请求体中**必须省略 `platform` 字段**
2. `Client-Metadata` 头中**必须省略 `platform` 字段**
3. 对于中国账号，`loadCodeAssist` 永远不会返回 project ID → **直接硬编码 `rising-fact-p41fc`，跳过 loadCodeAssist 步骤可行**
4. `User-Agent` 应使用完整 Electron/Chrome UA，不是简写的 `antigravity/{version} windows/amd64`
5. `generateContent` 验证完整通过，整个链路可行性已确认 ✅

### 最终验证数据

```
OAuth:           OK (jiangsimpler@gmail.com)
Token 交换:      OK (expires_in: 3599s)
Token 刷新:      OK
loadCodeAssist:  200 OK（无 project ID，使用硬编码）
Project ID:      rising-fact-p41fc（硬编码默认值，永久有效）
generateContent: OK - gemini-3-flash 正常响应
```
