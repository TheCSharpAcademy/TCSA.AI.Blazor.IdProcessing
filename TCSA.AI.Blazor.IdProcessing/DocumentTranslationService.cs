using Azure;
using Azure.AI.TextAnalytics;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System.Text;

namespace TCSA.AI.Blazor.IdProcessing;

public interface IDocumentTranslationService
{
    Task<string> TranslateDocument(IBrowserFile file);
}

public class DocumentTranslationService : IDocumentTranslationService
{
    private readonly IConfiguration _configuration;

    public DocumentTranslationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> TranslateDocument(IBrowserFile file)
    {
        // Load API credentials from configuration
        string visionEndpoint = _configuration["Values:ComputerVisionEndpoint"];
        string visionKey = _configuration["Values:ComputerVisionKey"];

        string analyticsEndpoint = _configuration["Values:TextAnalyticsEndpoint"];
        string analyticsKey = _configuration["Values:TextAnalyticsKey"];

        string translationEndpoint = "https://api.cognitive.microsofttranslator.com";
        string translationKey = _configuration["Values:TranslationKey"];
        string translationArea = _configuration["Values:AzureRegion"];

        // Step 1: Extract Text Using Azure Vision Image Analysis
        var client = new ImageAnalysisClient(new Uri(visionEndpoint), new AzureKeyCredential(visionKey));

        await using var stream = file.OpenReadStream(10 * 1024 * 1024); // Limit to 10MB

        if (stream.Length == 0)
        {
            throw new Exception("The uploaded file is empty.");
        }

        Console.WriteLine(stream.Length);

        var imageData = await BinaryData.FromStreamAsync(stream);

        var analysisOptions = new ImageAnalysisOptions
        {
            // You can specify additional options here
        };

        var result = await client.AnalyzeAsync(imageData, VisualFeatures.Read, new ImageAnalysisOptions());

        string extractedText = string.Join(" ", result.Value.Read.Blocks
                .SelectMany(block => block.Lines)
                .Select(line => line.Text));

        Console.WriteLine(extractedText);

        var textClient = new TextAnalyticsClient(new Uri(analyticsEndpoint), new AzureKeyCredential(analyticsKey));
        var languageResult = await textClient.DetectLanguageAsync(extractedText);

        string detectedLanguage = languageResult.Value.Iso6391Name;

        string route = $"/translate?api-version=3.0&from={detectedLanguage}&to=en";
        string textToTranslate = extractedText;
        object[] body = new object[] { new { Text = textToTranslate } };
        var requestBody = JsonConvert.SerializeObject(body);


        using (var httpClient = new HttpClient())
        using (var request = new HttpRequestMessage())
        {
            // Build the request.
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(translationEndpoint + route);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", translationKey);
            // location required if you're using a multi-service or regional (not global) resource.
            request.Headers.Add("Ocp-Apim-Subscription-Region", translationArea);

            // Send the request and get response.
            HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            // Read response as a string.
            string res = await response.Content.ReadAsStringAsync();
            Console.WriteLine(res);

            return res;
        }
    }
}
