using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimplerJiangAiAgent.Api.Modules;

public interface IModule
{
    // 模块注册服务
    void Register(IServiceCollection services, IConfiguration configuration);

    // 模块路由映射
    void MapEndpoints(IEndpointRouteBuilder app);
}
