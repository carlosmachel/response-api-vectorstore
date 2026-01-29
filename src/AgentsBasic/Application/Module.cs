using Microsoft.AspNetCore.Mvc;

namespace AgentsBasic.Application;

public static class Module
{
    public static void Register(this IEndpointRouteBuilder app)
    {
        app.MapPost("/ai-agent", async (
                [FromServices] Service service,
                [FromQuery] string name, 
                [FromQuery] string instructions,
                [FromQuery] string vectorStoreId) =>
            {
                var agentId = await service.CreateAgentAsync(
                    name, 
                    instructions,
                    vectorStoreId);
                return Results.Ok(agentId);
            })
            .WithTags("Ai Agents");
        
        
        app.MapGet("/ai-agent/create-conversation", async (
                [FromServices] Service service) =>
            {
                var agentId = await service.CreateConversationAsync();
                return Results.Ok(agentId);
            })
            .WithTags("Ai Agents");
        
        app.MapPost("/ai-agent/upload-file", async (
                [FromServices] Service service,
                [FromForm] IFormFile file) =>
            {
                if (file.Length == 0)
                {
                    return Results.BadRequest("Arquivo vazio.");
                }

                await using var stream = file.OpenReadStream();
                var fileId = await service.UploadFileAsync(stream, file.FileName);
                return Results.Ok(fileId);
            })
            .DisableAntiforgery()
            .WithTags("Ai Agents");
        
        app.MapPost("/ai-agent/create-vectorstore", async (
                [FromQuery] string fileId,
                [FromQuery] string vectorStoreName, 
                [FromServices] Service service) =>
            {
                var vectorStoreId = await service.CreateVectorStoreAsync(vectorStoreName, fileId);
                return Results.Ok(vectorStoreId);
            })
            .WithTags("Ai Agents");
        
        app.MapGet("/ai-agent/response", async (
                [FromServices] Service service,
                [FromQuery] string agentName,
                [FromQuery] string conversationId,
                [FromQuery] string vectorStoreId,
                [FromQuery] string userInput) =>
            {
                var result = await service.ResponseAsync(agentName, conversationId, userInput, vectorStoreId);
                return Results.Ok(result);
            })
            .WithTags("Ai Agents");
    }
}
