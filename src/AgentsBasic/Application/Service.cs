using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

#pragma warning disable OPENAI001
#pragma warning disable CA2252

namespace AgentsBasic.Application;

public record AgentResult(string AgentName, string Version);

public record ResponseResult(string OutputText, string Id);

public class Service(IOptions<AzureAiSettings> settings)
{
    private AIProjectClient GetProjectClient()
    {
        return new AIProjectClient(
            new Uri(settings.Value.Uri),  
            new DefaultAzureCredential());  
    }

    public async Task<AgentResult> CreateAgentAsync(
        string agentName,
        string instructions,
        string? vectorStoreId)
    {
        var client = GetProjectClient();
        var creationOptions = new AgentVersionCreationOptions(
            new PromptAgentDefinition(model: settings.Value.Model)
            {
                Instructions = instructions,
                Tools = { new FileSearchTool([vectorStoreId]) }
            });
        
        AgentVersion agent = await client.Agents.CreateAgentVersionAsync(agentName: agentName, creationOptions);
        
        return new AgentResult(agent.Name, agent.Version);
    }

    public virtual async Task<string> UploadFileAsync(
        Stream content,
        string fileName)
    {
        var client = GetProjectClient().GetProjectOpenAIClient();
        var fileClient = client.GetOpenAIFileClient();
        OpenAIFile result = await fileClient.UploadFileAsync(BinaryData.FromStream(content), fileName, FileUploadPurpose.Assistants);
        return result.Id;
    }
    
    public virtual async Task<string> CreateVectorStoreAsync(string vectorStoreName, string fileId)
    {
        var client = GetProjectClient().GetProjectOpenAIClient().GetVectorStoreClient();
        var options = new VectorStoreCreationOptions
        {
            Name = vectorStoreName,
            ExpirationPolicy = new VectorStoreExpirationPolicy(
                VectorStoreExpirationAnchor.LastActiveAt,
                365),
            FileIds = { fileId }
        };
        
        var response = await client.CreateVectorStoreAsync(options);
        var vectorStore = response.Value;
        return vectorStore.Id;
    }
    
    public async Task<string> CreateConversationAsync()
    {
        var client = GetProjectClient();
        var options = new ProjectConversationCreationOptions();
        var conversation = await client
            .OpenAI
            .Conversations
            .CreateProjectConversationAsync(options);
        return conversation.Value.Id;
    }
    
    public async Task<ResponseResult> ResponseAsync(
        string agentName,
        string conversationId,
        string userInput,
        string vectorStoreId)
    {
        var client = GetProjectClient();
        //AgentRecord record = await client.Agents.GetAgentAsync(agentName);
        
        var responseClient = client.OpenAI.GetProjectResponsesClientForModel(settings.Value.Model, conversationId);
        var options = new CreateResponseOptions(
            [ResponseItem.CreateUserMessageItem(userInput)],
            settings.Value.Model)
        {
            ConversationOptions = new ResponseConversationOptions(conversationId),
            Tools = { new FileSearchTool([vectorStoreId]) }
        };
        
        var result = await responseClient.CreateResponseAsync(options);
        return new ResponseResult(result.Value.GetOutputText(), result.Value.Id);
    }
}
