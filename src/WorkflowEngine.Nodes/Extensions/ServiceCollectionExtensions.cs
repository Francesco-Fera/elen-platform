using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Nodes.Credentials;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Registry;

namespace WorkflowEngine.Nodes.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNodeSystem(this IServiceCollection services)
    {
        services.AddSingleton<INodeRegistry, NodeRegistry>();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddScoped<ICredentialService, CredentialService>();
        services.AddScoped<ICredentialEncryptionService, CredentialEncryptionService>();

        services.AddHttpClient();

        return services;
    }
}