using Newtonsoft.Json;
using System.Text;

namespace TCSA.AI.Blazor.IdProcessing.Services;

public interface IDocumentTranslationService
{
    Task<string> TranslateText(string language, string text);
}

public class DocumentTranslationService : IDocumentTranslationService
{
    private readonly IConfiguration _configuration;

    public DocumentTranslationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> TranslateText(string language, string text)
    {
        string translationEndpoint = "https://api.cognitive.microsofttranslator.com";
        string translationKey = _configuration["Values:TranslationKey"];
        string translationArea = _configuration["Values:AzureRegion"];

        string route = $"/translate?api-version=3.0&from={language}&to=en";
        object[] body = new object[] { new { Text = text } };
        var requestBody = JsonConvert.SerializeObject(body);

        var result = "";
        using (var httpClient = new HttpClient())
        using (var request = new HttpRequestMessage())
        {
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(translationEndpoint + route);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", translationKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", translationArea);


            HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            string res = await response.Content.ReadAsStringAsync();
            result = res;
            Console.WriteLine(res);
        }
        return result;
    }
}
