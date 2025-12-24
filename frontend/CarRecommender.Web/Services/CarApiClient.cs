using System.Net.Http.Json;
using System.Text.Json;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Services;

/// <summary>
/// Service voor het communiceren met de CarRecommender API.
/// Gebruikt HttpClient voor alle API calls.
/// </summary>
public class CarApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CarApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CarApiClient(HttpClient httpClient, ILogger<CarApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Haalt alle auto's op via GET /api/cars
    /// </summary>
    public async Task<List<Car>?> GetAllCarsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/cars");
            response.EnsureSuccessStatusCode();
            
            var cars = await response.Content.ReadFromJsonAsync<List<Car>>(_jsonOptions);
            return cars;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto's van de API");
            throw;
        }
    }

    /// <summary>
    /// Haalt een specifieke auto op via GET /api/cars/{id}
    /// </summary>
    public async Task<Car?> GetCarByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cars/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Car>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto {CarId} van de API", id);
            throw;
        }
    }

    /// <summary>
    /// Haalt recommendations op voor een specifieke auto via GET /api/recommendations/{id}?top={top}
    /// </summary>
    public async Task<List<RecommendationResult>?> GetRecommendationsAsync(int carId, int top = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recommendations/{carId}?top={top}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations voor auto {CarId}", carId);
            throw;
        }
    }

    /// <summary>
    /// Stuurt een POST naar /api/recommendations/text met tekst input
    /// </summary>
    public async Task<List<RecommendationResult>?> GetRecommendationsFromTextAsync(string text, int top = 5)
    {
        try
        {
            var request = new RecommendationTextRequest
            {
                Text = text,
                Top = top
            };

            var response = await _httpClient.PostAsJsonAsync("/api/recommendations/text", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations op basis van tekst: {Text}", text);
            throw;
        }
    }

    /// <summary>
    /// Haalt alle images op voor een specifieke auto via GET /api/cars/{id}/images
    /// </summary>
    public async Task<List<string>> GetCarImagesAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cars/{id}/images");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<string>();
            }
            
            response.EnsureSuccessStatusCode();
            var images = await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions);
            return images ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van images voor auto {CarId} van de API", id);
            return new List<string>();
        }
    }

    /// <summary>
    /// Stuurt een POST naar /api/recommendations/hybrid/manual met manuele filters
    /// VERSCHIL MET TEKST MODUS: Geen tekst parsing, directe formulier velden
    /// </summary>
    public async Task<List<RecommendationResult>?> GetRecommendationsFromManualFiltersAsync(ManualFilterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/recommendations/hybrid/manual", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations op basis van manuele filters");
            throw;
        }
    }
}


