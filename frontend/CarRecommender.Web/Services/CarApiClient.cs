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
        // #region agent log
        // DEBUG HYPOTHESIS E: API call entry
        try {
            var workspacePath = @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New";
            var logPath = Path.Combine(workspacePath, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var logEntry = new {
                location = "CarApiClient.cs:GetCarByIdAsync",
                message = "API call entry",
                data = new { id = id, baseUrl = _httpClient.BaseAddress?.ToString() },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "E"
            };
            await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to write debug log");
        }
        // #endregion
        
        try
        {
            var url = $"/api/cars/{id}";
            
            // #region agent log
            // DEBUG HYPOTHESIS E: URL constructed
            try {
                var workspacePath = @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New";
                var logPath = Path.Combine(workspacePath, ".cursor", "debug.log");
                var logEntry2 = new {
                    location = "CarApiClient.cs:GetCarByIdAsync",
                    message = "URL constructed",
                    data = new { id = id, url = url, fullUrl = _httpClient.BaseAddress + url },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "E"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry2) + Environment.NewLine);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to write debug log");
            }
            // #endregion
            
            var response = await _httpClient.GetAsync(url);
            
            // #region agent log
            // DEBUG HYPOTHESIS E: Response received
            try {
                var workspacePath = @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New";
                var logPath = Path.Combine(workspacePath, ".cursor", "debug.log");
                var logEntry3 = new {
                    location = "CarApiClient.cs:GetCarByIdAsync",
                    message = "Response received",
                    data = new { id = id, statusCode = (int)response.StatusCode, isNotFound = response.StatusCode == System.Net.HttpStatusCode.NotFound },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "E"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry3) + Environment.NewLine);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to write debug log");
            }
            // #endregion
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            var car = await response.Content.ReadFromJsonAsync<Car>(_jsonOptions);
            
            // #region agent log
            // DEBUG HYPOTHESIS E: Car deserialized
            try {
                var workspacePath = @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New";
                var logPath = Path.Combine(workspacePath, ".cursor", "debug.log");
                var logEntry4 = new {
                    location = "CarApiClient.cs:GetCarByIdAsync",
                    message = "Car deserialized",
                    data = new { id = id, carFound = car != null, carId = car?.Id, carBrand = car?.Brand },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "E"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry4) + Environment.NewLine);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to write debug log");
            }
            // #endregion
            
            return car;
        }
        catch (HttpRequestException ex)
        {
            // #region agent log
            // DEBUG HYPOTHESIS E: Exception caught
            try {
                var workspacePath = @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New";
                var logPath = Path.Combine(workspacePath, ".cursor", "debug.log");
                var logEntry5 = new {
                    location = "CarApiClient.cs:GetCarByIdAsync",
                    message = "Exception caught",
                    data = new { id = id, exception = ex.Message, stackTrace = ex.StackTrace },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "E"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry5) + Environment.NewLine);
            } catch {}
            // #endregion
            
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
        // #region agent log
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logEntry = new {
                location = "CarApiClient.cs:GetRecommendationsFromTextAsync",
                message = "Method entry",
                data = new { text = text?.Substring(0, Math.Min(50, text?.Length ?? 0)), top = top },
                timestamp = startTime,
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "D"
            };
            await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
        // #endregion
        try
        {
            var request = new RecommendationTextRequest
            {
                Text = text,
                Top = top
            };

            // #region agent log
            var beforeRequest = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // #endregion
            var response = await _httpClient.PostAsJsonAsync("/api/recommendations/text", request, _jsonOptions);
            // #region agent log
            var afterRequest = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logEntry = new {
                    location = "CarApiClient.cs:GetRecommendationsFromTextAsync",
                    message = "API response received",
                    data = new { statusCode = (int)response.StatusCode, requestDurationMs = afterRequest - beforeRequest },
                    timestamp = afterRequest,
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
            } catch {}
            // #endregion
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            // #region agent log
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logEntry = new {
                    location = "CarApiClient.cs:GetRecommendationsFromTextAsync",
                    message = "HttpRequestException",
                    data = new { error = ex.Message, innerException = ex.InnerException?.Message },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
            } catch {}
            // #endregion
            _logger.LogError(ex, "Fout bij het ophalen van recommendations op basis van tekst: {Text}", text);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            // #region agent log
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logEntry = new {
                    location = "CarApiClient.cs:GetRecommendationsFromTextAsync",
                    message = "TaskCanceledException (timeout)",
                    data = new { error = ex.Message },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D"
                };
                await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
            } catch {}
            // #endregion
            _logger.LogError(ex, "Timeout bij het ophalen van recommendations op basis van tekst: {Text}", text);
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
    /// </summary>
    public async Task<MlEvaluationResult?> GetMlEvaluationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/ml/evaluation");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<MlEvaluationResult>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van ML evaluatie resultaten");
            throw;
        }
    }
}


