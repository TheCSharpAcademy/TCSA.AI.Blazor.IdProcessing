using Azure.AI.Vision.ImageAnalysis;
using Azure;
using Microsoft.AspNetCore.Components.Forms;
using TCSA.AI.Blazor.IdProcessing.Data;

namespace TCSA.AI.Blazor.IdProcessing.Services;

public interface IImageAnalysisService
{
    Task<Guest> ExtractText(IBrowserFile browserFile);
}

public class ImageAnalysisService: IImageAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly IOpenAIService _openAIService;

    public ImageAnalysisService(IConfiguration configuration, IOpenAIService openAIService)
    {
        _configuration = configuration;
        _openAIService = openAIService;
    }

    public async Task<Guest> ExtractText(IBrowserFile file) {
        string visionEndpoint = _configuration["Values:ComputerVisionEndpoint"];
        string visionKey = _configuration["Values:ComputerVisionKey"];

        var client = new ImageAnalysisClient(new Uri(visionEndpoint), new AzureKeyCredential(visionKey));

        await using var stream = file.OpenReadStream(10 * 1024 * 1024); // Limit to 10MB

        if (stream.Length == 0)
        {
            throw new Exception("The uploaded file is empty.");
        }

        Console.WriteLine(stream.Length);

        var imageData = await BinaryData.FromStreamAsync(stream);

        var result = await client.AnalyzeAsync(imageData, VisualFeatures.Read, new ImageAnalysisOptions());

        string extractedText = string.Join(" ", result.Value.Read.Blocks
                .SelectMany(block => block.Lines)
                .Select(line => line.Text));

        var res = await _openAIService.GetAzureOpenAIAnswerAsync(extractedText);

        return res;
    }
}
