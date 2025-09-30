using WorkflowEngine.Execution.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface ITopologicalSorter
{
    List<string> Sort(WorkflowGraph graph);
    List<List<string>> GetParallelGroups(WorkflowGraph graph);
}