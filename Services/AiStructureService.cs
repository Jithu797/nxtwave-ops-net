using System.Text.Json;

namespace LMSDashboard.Services;

public interface IAiStructureService
{
    Task<object> StructureAsync(object[] rows);
}

public class AiStructureService : IAiStructureService
{
    private readonly HttpClient _http;
    private readonly ILogger<AiStructureService> _logger;
    private readonly string _baseUrl;

    public AiStructureService(IHttpClientFactory factory, ILogger<AiStructureService> logger, IConfiguration config)
    {
        _http = factory.CreateClient("AiService");
        _logger = logger;
        _baseUrl = config["AiService:BaseUrl"] ?? "http://localhost:8001";
    }

    public async Task<object> StructureAsync(object[] rows)
    {
        var payload = new { rows };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync($"{_baseUrl}/ai/structure", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(responseJson) ?? new { };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI structure service call failed.");
            throw;
        }
    }
}
