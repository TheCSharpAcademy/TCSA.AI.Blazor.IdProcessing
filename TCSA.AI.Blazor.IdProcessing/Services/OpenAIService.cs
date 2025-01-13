using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using TCSA.AI.Blazor.IdProcessing.Data;

namespace TCSA.AI.Blazor.IdProcessing.Services;

public interface IOpenAIService
{
    Task<Guest> GetAzureOpenAIAnswerAsync(string question);
}

public class OpenAIService: IOpenAIService
{
    private readonly string _openAiApiKey;
    private readonly Uri _openAiEndpoint;

    // Constructor that gets API Key and Endpoint from appsettings.json
    public OpenAIService(IConfiguration configuration)
    {
        _openAiApiKey = configuration["Values:OpenAIKey"];
        _openAiEndpoint = new Uri(configuration["Values:OpenAIEndpoint"]);
    }

    public async Task<Guest> GetAzureOpenAIAnswerAsync(string jsonInput)
    {

        string prompt = $"Generate a JSON response for a guest with the following details: {jsonInput}. The JSON should match this structure, all values translated to english. For dates, use yyyy-MM-dd:\n" +
                           "{\n" +
                           "    \"FirstName\": \"<string>\",\n" +
                           "    \"LastName\": \"<string>\",\n" +
                           "    \"DateOfBirth\": \"<string>\",\n" +
                           "    \"Address\": \"<string>\",\n" +
                           "    \"Country\": \"<string>\",\n" +
                           "    \"CheckInDate\": \"<string>\"\n" +
                           "}";

        //string prompt = $"Give me a paragraph summarizing information about this text: {jsonInput}.";

        AzureOpenAIClient azureClient = new(
           _openAiEndpoint,
            new ApiKeyCredential(_openAiApiKey));
        ChatClient chatClient = azureClient.GetChatClient("gpt-35-turbo");

        try
        {
            ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(prompt);
            Console.WriteLine(response.Value.Content[0].Text);
            var content =  response.Value.Content[0].Text;
            var guest = JsonSerializer.Deserialize<Guest>(content);
            return guest;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        // Create the Completions request with your question

        return null;
    }
}
