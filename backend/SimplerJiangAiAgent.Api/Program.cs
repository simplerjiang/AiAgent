using SimplerJiangAiAgent.Api.Infrastructure.Config;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using SimplerJiangAiAgent.Api.Infrastructure.Security;
using SimplerJiangAiAgent.Api.Modules;

var builder = WebApplication.CreateBuilder(args);

// 服务注册
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    // 桌面端嵌入与本地开发使用的宽松策略（生产环境请收敛）
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.Configure<ConfigCenterOptions>(builder.Configuration.GetSection(ConfigCenterOptions.SectionName));
builder.Services.Configure<PermissionOptions>(builder.Configuration.GetSection(PermissionOptions.SectionName));
builder.Services.AddSingleton<IPermissionService, PermissionService>();

builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

// 中间件管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

// 基础健康检查
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithOpenApi();

app.MapModules();

app.Run();
