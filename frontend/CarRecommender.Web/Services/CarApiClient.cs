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
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Haalt alle auto's op via GET /api/cars
    /// Backend retourneert een PagedResult met max pageSize van 100, dus we moeten pagineren om alle auto's op te halen.
    /// </summary>
    public async Task<List<Car>?> GetAllCarsAsync()
    {
        try
        {
            // Backend heeft max pageSize van 100, dus we moeten pagineren
            // Haal eerst de eerste pagina op om te weten hoeveel pagina's er zijn
            var firstPageResponse = await _httpClient.GetAsync("/api/cars?page=1&pageSize=100");
            firstPageResponse.EnsureSuccessStatusCode();
            
            var firstPageResult = await firstPageResponse.Content.ReadFromJsonAsync<PagedResult<Car>>(_jsonOptions);
            if (firstPageResult == null)
                return new List<Car>();
            
            var allCars = new List<Car>(firstPageResult.Items);
            
            // Haal de resterende pagina's op
            for (int page = 2; page <= firstPageResult.TotalPages; page++)
            {
                var response = await _httpClient.GetAsync($"/api/cars?page={page}&pageSize=100");
                response.EnsureSuccessStatusCode();
                
                var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Car>>(_jsonOptions);
                if (pagedResult?.Items != null)
                {
                    allCars.AddRange(pagedResult.Items);
                }
            }
            
            return allCars;
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
            var url = $"/api/cars/{id}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            var car = await response.Content.ReadFromJsonAsync<Car>(_jsonOptions);
            
            return car;
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

            // Log de volledige URL die wordt gebruikt
            var fullUrl = $"{_httpClient.BaseAddress}api/recommendations/text";
            _logger.LogInformation("[CarApiClient] Aanroepen API: {FullUrl}", fullUrl);
            _logger.LogInformation("[CarApiClient] Request: Text={Text}, Top={Top}", text, top);

            var response = await _httpClient.PostAsJsonAsync("/api/recommendations/text", request, _jsonOptions);
            
            _logger.LogInformation("[CarApiClient] Response Status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
            _logger.LogInformation("[CarApiClient] Aantal recommendations ontvangen: {Count}", result?.Count ?? 0);
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP fout bij het ophalen van recommendations. BaseAddress: {BaseAddress}, URL: {Url}", 
                _httpClient.BaseAddress, "/api/recommendations/text");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout bij het ophalen van recommendations. BaseAddress: {BaseAddress}", 
                _httpClient.BaseAddress);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onverwachte fout bij het ophalen van recommendations. BaseAddress: {BaseAddress}", 
                _httpClient.BaseAddress);
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

    /// <summary>
    /// Haalt ML evaluatie resultaten op via GET /api/ml/evaluation
    /// ML evaluatie kan lang duren (30-60 seconden), gebruik langere timeout
    /// </summary>
    public async Task<MlEvaluationResult?> GetMlEvaluationAsync()
    {
        try
        {
            _logger.LogInformation("[CarApiClient] Start ML evaluatie request naar {BaseAddress}api/ml/evaluation", _httpClient.BaseAddress);
            
            // ML evaluatie kan lang duren, gebruik langere timeout (120 seconden)
            // HttpClient timeout is al ingesteld op 120 seconden in Program.cs
            // Gebruik POST voor langlopende operaties (beter dan GET voor processing)
            var response = await _httpClient.PostAsync("/api/ml/evaluation", null);
            
            _logger.LogInformation("[CarApiClient] ML evaluatie response status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<MlEvaluationResult>(_jsonOptions);
            
            if (result != null)
            {
                _logger.LogInformation("[CarApiClient] ML evaluatie resultaat ontvangen. IsValid: {IsValid}, TrainingSet: {TrainingSize}, TestSet: {TestSize}", 
                    result.IsValid, result.TrainingSetSize, result.TestSetSize);
            }
            
            return result;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "[CarApiClient] Timeout bij ML evaluatie (duurt langer dan {Timeout} seconden). BaseAddress: {BaseAddress}", 
                _httpClient.Timeout.TotalSeconds, _httpClient.BaseAddress);
            throw new HttpRequestException($"ML evaluatie duurt te lang (timeout na {_httpClient.Timeout.TotalSeconds} seconden). Dit kan 30-60 seconden duren. Probeer het later opnieuw.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CarApiClient] HTTP fout bij ML evaluatie. BaseAddress: {BaseAddress}, URL: {Url}", 
                _httpClient.BaseAddress, "/api/ml/evaluation");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CarApiClient] Onverwachte fout bij ML evaluatie. BaseAddress: {BaseAddress}", 
                _httpClient.BaseAddress);
            throw new HttpRequestException("Er is een onverwachte fout opgetreden bij het ophalen van ML evaluatie resultaten.", ex);
        }
    }

    /// <summary>
    /// Haalt ML model training status op via GET /api/ml/status
    /// </summary>
    public async Task<MlModelStatus?> GetMlStatusAsync()
    {
        try
        {
            _logger.LogInformation("[CarApiClient] Start ML status request naar {BaseAddress}api/ml/status", _httpClient.BaseAddress);
            
            var response = await _httpClient.GetAsync("/api/ml/status");
            
            _logger.LogInformation("[CarApiClient] ML status response status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<MlModelStatus>(_jsonOptions);
            
            if (result != null)
            {
                _logger.LogInformation("[CarApiClient] ML status ontvangen. IsTrained: {IsTrained}, LastTraining: {LastTraining}", 
                    result.IsTrained, result.LastTrainingTime);
            }
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CarApiClient] HTTP fout bij ML status. BaseAddress: {BaseAddress}, URL: {Url}", 
                _httpClient.BaseAddress, "/api/ml/status");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CarApiClient] Onverwachte fout bij ML status. BaseAddress: {BaseAddress}", 
                _httpClient.BaseAddress);
            throw new HttpRequestException("Er is een onverwachte fout opgetreden bij het ophalen van ML model status.", ex);
        }
    }

    /// <summary>
    /// Haalt aggregated ratings op voor een auto via GET /api/ratings/car/{carId}
    /// </summary>
    public async Task<AggregatedRating?> GetRatingsForCarAsync(int carId)
    {
        try
        {
            // Voeg cache-busting parameter toe om altijd verse data te krijgen
            var url = $"/api/ratings/car/{carId}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Voorkom caching
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AggregatedRating>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van ratings voor auto {CarId}", carId);
            return null;
        }
    }

    /// <summary>
    /// Haalt de rating op van de huidige gebruiker voor een specifieke auto via GET /api/ratings/user/{userId}/car/{carId}
    /// </summary>
    public async Task<int?> GetUserRatingForCarAsync(string userId, int carId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/ratings/user/{userId}/car/{carId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            var rating = await response.Content.ReadFromJsonAsync<UserRating>(_jsonOptions);
            return rating?.Rating;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van user rating voor auto {CarId}", carId);
            return null;
        }
    }

    /// <summary>
    /// Voegt een rating toe via POST /api/ratings
    /// </summary>
    public async Task<bool> AddRatingAsync(RatingRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ratings", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het toevoegen van rating voor auto {CarId}", request.CarId);
            return false;
        }
    }
}



