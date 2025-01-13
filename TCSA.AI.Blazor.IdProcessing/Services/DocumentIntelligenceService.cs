using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using TCSA.AI.Blazor.IdProcessing.Data;

namespace TCSA.AI.Blazor.IdProcessing.Services;

public interface IDocumentIntelligenceService
{
    Task<Guest> ExtractDataFromId(IBrowserFile file);
}

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly IConfiguration _configuration;
    private readonly ITextAnalyticsService _textAnalyticsService;
    private readonly IImageAnalysisService _imageAnalysisService;
    private readonly IDocumentTranslationService _documentTranslationService;

    public DocumentIntelligenceService(
        IConfiguration configuration, 
        IDocumentTranslationService documentTranslationService, 
        ITextAnalyticsService textAnalyticsService,
        IImageAnalysisService imageAnalysisService,
        IOpenAIService openAIService)
    {
        _configuration = configuration;
        _documentTranslationService = documentTranslationService;
        _textAnalyticsService = textAnalyticsService;
        _imageAnalysisService = imageAnalysisService;
    }

    public async Task<Guest> ExtractDataFromId(IBrowserFile file)
    {
        var guest = new Guest();

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

            MapDocument(guest, result);

            var extractedValues = ExtractValues(guest);
            var language = await _textAnalyticsService.ExtractLanguage(extractedValues);

            var languages = new[] { "ja" };

            if (language.ToLower() == "ru")
            {
                return await TranslateGuest(language, guest);
            }

            if (languages.Contains(language) || extractedValues == "0")
            {
                return await _imageAnalysisService.ExtractText(file);
            }

            return guest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    private static void MapDocument(Guest guest, AnalyzeResult result)
    {
        var idDocument = result.Documents.FirstOrDefault();
        var fieldsIdentified = 0;

        if (idDocument != null)
        {
            Console.WriteLine("Fields identified in the document:");
            foreach (var field in idDocument.Fields)
            {
                fieldsIdentified++;
                string fieldName = field.Key;
                var fieldValue = field.Value?.Content;
                Console.WriteLine($"{fieldName}: {fieldValue}");
            }
        }

        else
        {
            Console.WriteLine("No ID document data extracted.");
        }
        guest.FirstName = idDocument.Fields.TryGetValue("FirstName", out var firstNameField) && firstNameField?.Content != null
                        ? firstNameField.Content
                        : string.Empty;

        guest.LastName = idDocument.Fields.TryGetValue("LastName", out var lastNameField) && lastNameField?.Content != null
            ? lastNameField.Content
            : string.Empty;
        guest.Address = idDocument.Fields.TryGetValue("Address", out var addressField) && addressField?.Content != null
            ? addressField.Content
            : string.Empty;
        guest.CheckInDate = DateTime.UtcNow;

        if (idDocument.Fields.TryGetValue("DateOfBirth", out var dobField) && dobField?.Content != null)
        {
            string dobString = dobField.Content;
            string[] formats = { "MM/dd/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd MMMM yyyy", "dd MMM yyyy", "dd.MM.yyyy", "dd/mm/yyyy" };
            if (DateTime.TryParseExact(dobString, formats, null, System.Globalization.DateTimeStyles.None, out var parsedDob))
            {
                guest.DateOfBirth = parsedDob;
            }
            else
            {
                Console.WriteLine($"Invalid Date of Birth format: {dobString}");
                guest.DateOfBirth = DateTime.MinValue; // Or handle invalid DOB as per your logic
            }
        }
        else
        {
            guest.DateOfBirth = DateTime.MinValue; // Handle case where DOB field is not present
        }
    }

    private async Task<Guest> TranslateGuest(string language, Guest guest)
    {
        var translatedGuest = new Guest();
        string jsonString = JsonConvert.SerializeObject(guest, Formatting.Indented);

        string translatedJsonString = await _documentTranslationService.TranslateText(language, jsonString);
        translatedGuest = ConvertJsonToGuestModel(translatedJsonString);

        return translatedGuest;
    }

    public string ExtractValues(Guest guest)
    {
        var guestValues = new List<string>();
        var properties = guest.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (property.Name == "DateOfBirth" || property.Name == "CheckInDate")
            {
                continue;
            }
            // Step 2: Extract the value of each property (assumes the value is a string)
            var value = property.GetValue(guest)?.ToString();

            if (!string.IsNullOrEmpty(value))
            {
                // Step 3: Add the value to the list
                guestValues.Add(value);
            }
        }

        // Step 4: Join all the values into a single string, separated by spaces
        return string.Join(" ", guestValues);
    }

    public Guest ConvertJsonToGuestModel(string jsonResponse)
    {
        // Deserialize the outer JSON structure that contains the translations
        var translationResponse = JsonConvert.DeserializeObject<List<TranslationResponse>>(jsonResponse);

        // Get the translated text (which is a JSON string of the Guest model)
        var translatedText = translationResponse?[0]?.Translations?[0]?.Text;

        if (!string.IsNullOrEmpty(translatedText))
        {

            var guest = JsonConvert.DeserializeObject<Guest>(translatedText);

            if (guest.FirstName.Contains("Patronymic", StringComparison.OrdinalIgnoreCase))
            {
                guest.FirstName = guest.FirstName.Replace("Patronymic", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (guest.LastName.Contains("Name", StringComparison.OrdinalIgnoreCase))
            {
                guest.LastName = guest.LastName.Replace("Name", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return guest;
        }

        return null;
    }

    public class TranslationResponse
    {
        public List<Translation> Translations { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public string To { get; set; }
    }
}
