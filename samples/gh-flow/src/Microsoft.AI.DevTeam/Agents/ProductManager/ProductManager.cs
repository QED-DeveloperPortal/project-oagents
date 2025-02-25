using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class ProductManager : AiAgent<ProductManagerState>, IManageProducts
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly ILogger<ProductManager> _logger;

    public ProductManager([PersistentState("state", "messages")] IPersistentState<AgentState<ProductManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<ProductManager> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.ReadmeRequested):
            {
                var context = item.ToGithubContext();
                var readme = await CreateReadme(item.Data["input"]);
                var data = context.ToData();
                data["result"]=readme;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                     Type = nameof(GithubFlowEventType.ReadmeGenerated),
                     Subject = context.Subject,
                     Data = data
                });
            }
                
                break;
            case nameof(GithubFlowEventType.ReadmeChainClosed):
            {
                var context = item.ToGithubContext();
                var lastReadme = _state.State.History.Last().Message;
                var data = context.ToData();
                data["readme"] = lastReadme;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                     Type = nameof(GithubFlowEventType.ReadmeCreated),
                     Subject = context.Subject,
                    Data = data
                });
            }
                
                break;
            default:
                break;
        }
    }

    public async Task<string> CreateReadme(string ask)
    {
        var x = 0;
        try
        {
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask)};
            x = 1;
            var instruction = "Consider the following architectural guidelines:!waf!";
            x = 2;
            var enhancedContext = await AddKnowledge(instruction, "waf",context);
            x = 3;
            return await CallFunction(PMSkills.Readme, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating readme {x}");
            return default;
        }
    }
}

public interface IManageProducts
{
    public Task<string> CreateReadme(string ask);
}

[GenerateSerializer]
public class ProductManagerState
{
    [Id(0)]
    public string Capabilities { get; set; }
}