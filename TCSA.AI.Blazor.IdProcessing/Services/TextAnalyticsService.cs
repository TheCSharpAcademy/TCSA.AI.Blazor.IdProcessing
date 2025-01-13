using Azure.AI.TextAnalytics;
using Azure;

namespace TCSA.AI.Blazor.IdProcessing.Services;

public interface ITextAnalyticsService 
{
    Task<string> ExtractLanguage(string text);
}
public class TextAnalyticsService : ITextAnalyticsService
{
    private readonly IConfiguration _configuration;

    public TextAnalyticsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task<string> ExtractLanguage(string text)
    {
        string analyticsEndpoint = _configuration["Values:TextAnalyticsEndpoint"];
        string analyticsKey = _configuration["Values:TextAnalyticsKey"];

        var textClient = new TextAnalyticsClient(new Uri(analyticsEndpoint), new AzureKeyCredential(analyticsKey));

        var languageResult = await textClient.DetectLanguageAsync(text);

        return languageResult.Value.Iso6391Name;
    }
}
