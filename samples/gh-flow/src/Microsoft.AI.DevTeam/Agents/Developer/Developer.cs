using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

using Microsoft.AI.DevTeam.Utilities;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Dev : AiAgent<DeveloperState>, IDevelopApps
{
    protected override string Namespace => Consts.MainNamespace;

    private readonly ILogger<Dev> _logger;

    public Dev([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Dev> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.CodeGenerationRequested):
                {
                    var context = item.ToGithubContext();
                    var code = await GenerateCode(item.Data["input"]);
                    var data = context.ToData();
                    data["result"] = code;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.CodeGenerated),
                        Subject = context.Subject,
                        Data = data
                    });
                }

                break;
            case nameof(GithubFlowEventType.CodeChainClosed):
                {
                    var context = item.ToGithubContext();
                    var lastCode = _state.State.History.Last().Message;
                    var data = context.ToData();
                    data["code"] = lastCode;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.CodeCreated),
                        Subject = context.Subject,
                        Data = data
                    });
                }

                break;
            default:
                break;
        }
    }

    public async Task<string> GenerateCode(string ask)
    {
        try
        {
            _logger.LogInformation($"DEV CREATEPLAN PRELUDE {_kernel.Plugins.Count} {ConstantUtils.Messages.Count}");

            foreach (var m in ConstantUtils.Messages)
            {
                _logger.LogInformation($"DEV CONSTANTUTILS.MESSAGES: {m}");
            }

            foreach (var p in _kernel.Plugins)
            {
                _logger.LogInformation($"DEV CREATEPLAN: {p.Name}");
                _logger.LogInformation($"DEV CREATEPLAN Description: {p.Description}");
                _logger.LogInformation($"DEV CREATEPLAN: {p.FunctionCount}");
                _logger.LogInformation($"DEV CREATEPLAN: {p["get_app_catalog_groups"]}");
                var f = p["get_app_catalog_groups"];
                _logger.LogInformation($"DEV CREATEPLAN Function Description: {f.Description}");
                _logger.LogInformation($"DEV CREATEPLAN Function Name: {f.Name}");
                _logger.LogInformation($"DEV CREATEPLAN Function PluginName: {f.PluginName}");
                // Currently returning null for f.Metadata and f.ExecutionSettings
                // _logger.LogInformation($"DEV CREATEPLAN Function Description: {f.Metadata}");
                // var es = f.ExecutionSettings;
                // foreach (var setting in es)
                //     _logger.LogInformation($"DEV CREATEPLAN Function Execution Setting: {setting.Key} Value: {setting.Value}");
            }

            // TODO: ask the architect for the high level architecture as well as the files structure of the project
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask) };
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf", context);
            //var settings = new OpenAIPromptExecutionSettings{
                 //ResponseFormat = "json_object",
            //     MaxTokens = 16384, //32768, 
                 //Temperature = 0.4,
                 //TopP = 1,
            //     ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            //};
            return await CallFunction(DeveloperSkills.Implement, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            return default;
        }
    }
}

[GenerateSerializer]
public class DeveloperState
{
    [Id(0)]
    public string Understanding { get; set; }
}

public interface IDevelopApps
{
    public Task<string> GenerateCode(string ask);
}

[GenerateSerializer]
public class UnderstandingResult
{
    [Id(0)]
    public string NewUnderstanding { get; set; }
    [Id(1)]
    public string Explanation { get; set; }
}