using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using Microsoft.AspNetCore.Components.Forms;

namespace TCSA.AI.Blazor.IdProcessing;

public interface IDocumentIntelligenceService
{
    Task ExtractDataFromId(IBrowserFile file);
}

public class DocumentIntelligenceService: IDocumentIntelligenceService
{
    private readonly IConfiguration _configuration;

    public DocumentIntelligenceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ExtractDataFromId(IBrowserFile file)
    {
        try
        {
            string endpoint = _configuration["Values:DocumentIntelligenceEndpoint"];
            string apiKey = _configuration["Values:DocumentIntelligenceKey"];

            var client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

            await using var nonSeekableStream = file.OpenReadStream(10 * 1024 * 1024); // 10 MB limit
            using var seekableStream = new MemoryStream();
            await nonSeekableStream.CopyToAsync(seekableStream);
            seekableStream.Position = 0; // Reset po
         
            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-idDocument", seekableStream);
            AnalyzeResult result = operation.Value;
           
            var idDocument = result.Documents.FirstOrDefault();

            if (idDocument.Fields.Count < 1) {

            }

            if (idDocument != null)
            {
                Console.WriteLine("Fields identified in the document:");
                foreach (var field in idDocument.Fields)
                {
                    string fieldName = field.Key; 
                    var fieldValue = field.Value?.Content; 
                    Console.WriteLine($"{fieldName}: {fieldValue}");
                }
            }
            else
            {
                Console.WriteLine("No ID document data extracted.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
