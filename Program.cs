using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<WorkflowService>();

var app = builder.Build();

app.MapPost("/workflow-definitions", (WorkflowDefinition def, WorkflowService service) =>
{
    var result = service.CreateDefinition(def);
    return result.IsSuccess ? Results.Ok(result.Message) : Results.BadRequest(result.Message);
});

app.MapGet("/workflow-definitions/{id}", (string id, WorkflowService service) =>
{
    var def = service.GetDefinition(id);
    return def is null ? Results.NotFound("Definition not found") : Results.Ok(def);
});

app.MapPost("/workflow-instances", (StartInstanceRequest req, WorkflowService service) =>
{
    var result = service.StartInstance(req.DefinitionId);
    return result.IsSuccess ? Results.Ok(result.Instance) : Results.BadRequest(result.Message);
});

app.MapPost("/workflow-instances/{id}/execute", (string id, ExecuteActionRequest req, WorkflowService service) =>
{
    var result = service.ExecuteAction(id, req.ActionId);
    return result.IsSuccess ? Results.Ok(result.Instance) : Results.BadRequest(result.Message);
});

app.MapGet("/workflow-instances/{id}", (string id, WorkflowService service) =>
{
    var instance = service.GetInstance(id);
    return instance is null ? Results.NotFound("Instance not found") : Results.Ok(instance);
});

app.Run();

public record State(string Id, string Name, bool IsInitial, bool IsFinal, bool Enabled);

public record ActionTransition(string Id, string Name, bool Enabled, List<string> FromStates, string ToState);

public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<State> States { get; set; } = new();
    public List<ActionTransition> Actions { get; set; } = new();
}

public class WorkflowInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentStateId { get; set; } = string.Empty;
    public List<(string ActionId, DateTime Timestamp)> History { get; set; } = new();
}

public class StartInstanceRequest
{
    public string DefinitionId { get; set; } = string.Empty;
}

public class ExecuteActionRequest
{
    public string ActionId { get; set; } = string.Empty;
}

public class InMemoryStore
{
    public Dictionary<string, WorkflowDefinition> Definitions { get; } = new();
    public Dictionary<string, WorkflowInstance> Instances { get; } = new();
}

public class WorkflowService
{
    private readonly InMemoryStore _store;

    public WorkflowService(InMemoryStore store)
    {
        _store = store;
    }

    public (bool IsSuccess, string Message) CreateDefinition(WorkflowDefinition def)
    {
        if (_store.Definitions.ContainsKey(def.Id))
            return (false, "Duplicate definition ID");

        if (def.States.Count(s => s.IsInitial) != 1)
            return (false, "Workflow must have exactly one initial state");

        var stateIds = def.States.Select(s => s.Id).ToHashSet();

        foreach (var action in def.Actions)
        {
            if (!stateIds.Contains(action.ToState) || action.FromStates.Any(fs => !stateIds.Contains(fs)))
                return (false, "Action contains unknown state");
        }

        _store.Definitions[def.Id] = def;
        return (true, "Definition created");
    }

    public WorkflowDefinition? GetDefinition(string id)
    {
        _store.Definitions.TryGetValue(id, out var def);
        return def;
    }

    public (bool IsSuccess, string Message, WorkflowInstance? Instance) StartInstance(string defId)
    {
        if (!_store.Definitions.TryGetValue(defId, out var def))
            return (false, "Definition not found", null);

        var initState = def.States.First(s => s.IsInitial);

        var instance = new WorkflowInstance
        {
            DefinitionId = defId,
            CurrentStateId = initState.Id
        };

        _store.Instances[instance.Id] = instance;
        return (true, "Instance started", instance);
    }

    public (bool IsSuccess, string Message, WorkflowInstance? Instance) ExecuteAction(string instanceId, string actionId)
    {
        if (!_store.Instances.TryGetValue(instanceId, out var instance))
            return (false, "Instance not found", null);

        var def = _store.Definitions[instance.DefinitionId];
        var state = def.States.First(s => s.Id == instance.CurrentStateId);

        if (state.IsFinal)
            return (false, "Instance already in final state", null);

        var action = def.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action is null)
            return (false, "Invalid action ID", null);

        if (!action.Enabled)
            return (false, "Action is disabled", null);

        if (!action.FromStates.Contains(instance.CurrentStateId))
            return (false, "Action not valid from current state", null);

        instance.CurrentStateId = action.ToState;
        instance.History.Add((actionId, DateTime.UtcNow));
        return (true, "Action executed", instance);
    }

    public WorkflowInstance? GetInstance(string id)
    {
        _store.Instances.TryGetValue(id, out var instance);
        return instance;
    }
}

