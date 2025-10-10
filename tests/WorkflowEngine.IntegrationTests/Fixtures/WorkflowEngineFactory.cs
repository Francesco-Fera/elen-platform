using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.IntegrationTests.Fixtures;

public class WorkflowEngineFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IWorkflowExecutionEngine CreateExecutionEngine()
    {
        return _serviceProvider.GetRequiredService<IWorkflowExecutionEngine>();
    }

    public INodeRegistry CreateNodeRegistry()
    {
        return _serviceProvider.GetRequiredService<INodeRegistry>();
    }

    public IExpressionEvaluator CreateExpressionEvaluator()
    {
        return _serviceProvider.GetRequiredService<IExpressionEvaluator>();
    }

    public WorkflowEngineDbContext GetDbContext()
    {
        return _serviceProvider.GetRequiredService<WorkflowEngineDbContext>();
    }

    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }
}