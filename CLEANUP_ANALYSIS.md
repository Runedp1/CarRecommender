# Cleanup Analysis - Car Recommendation System

## Active API Endpoints (MUST KEEP)

1. **GET /api/cars** - Uses: `ICarRepository` → `CarRepository`
2. **GET /api/cars/{id}** - Uses: `ICarRepository` → `CarRepository`
3. **GET /api/recommendations/{id}** - Uses: `IRecommendationService.RecommendSimilarCars()`
4. **POST /api/recommendations/text** - Uses: `IRecommendationService.RecommendFromTextAsync()`
5. **POST /api/recommendations/hybrid/manual** - Uses: `IRecommendationService.RecommendFromManualFilters()`
6. **GET /api/ml/evaluation** - Uses: `IMlEvaluationService.EvaluateModel()`
7. **GET /api/health** - Uses: `ICarRepository`

## Core Recommendation Pipeline

### Active Pipeline (NEW):
- `RecommendFromTextAsync()` → `AdvancedScoringService` → `CarFeatureVectorFactory` → `SimilarityService` → `RankingService`
- `RecommendFromManualFilters()` → `AdvancedScoringService` → (same as above)

### Legacy Pipeline (STILL USED):
- `RecommendSimilarCars()` → `RecommendationEngine.CalculateSimilarity()` (OLD, but still called by API)

## Unused/Dead Code Analysis

### Controllers (NOT registered in DI):
- ❌ **FeedbackController** - Uses `FeedbackTrackingService` which is NOT registered → Will fail if called
- ❌ **RatingsController** - Uses `IUserRatingRepository` which is NOT registered → Will fail if called  
- ⚠️ **HomeController** - Just welcome message, not essential

### Services (Optional/Unused):
- ❌ **FeedbackTrackingService** - Only used in `TrackRecommendations()` which does nothing if null
- ❌ **ModelRetrainingService** - Only used in `TryTriggerRetraining()` which does nothing if null
- ❌ **CollaborativeFilteringService** - DISABLED in code (commented out: "TEMPORARILY DISABLED FOR PERFORMANCE")
- ❌ **IUserRatingRepository** - Not used anywhere in RecommendationService
- ⚠️ **RecommendationEngine** - Only used in `RecommendSimilarCars()`, but new pipeline uses `AdvancedScoringService`

### Repositories (NOT registered):
- ❌ **IFeedbackRepository** / **FeedbackRepository** - Not registered, only used by FeedbackTrackingService
- ❌ **IUserRatingRepository** / **UserRatingRepository** - Not registered, only used by RatingsController

## Cleanup Plan

### Phase 1: Remove Unused Controllers
- Delete `FeedbackController.cs` (not registered, will fail)
- Delete `RatingsController.cs` (not registered, will fail)
- Delete `HomeController.cs` (not essential)

### Phase 2: Move Unused Services to Legacy
- Move to `src/Legacy/`:
  - `FeedbackTrackingService.cs`
  - `ModelRetrainingService.cs`
  - `CollaborativeFilteringService.cs`
  - `FeedbackRepository.cs`
  - `IFeedbackRepository.cs`
  - `UserRatingRepository.cs`
  - `IUserRatingRepository.cs`
  - `UserRating.cs`
  - `UserFeedback.cs`
  - `ModelPerformanceMonitor.cs`
  - `SessionUserService.cs`

### Phase 3: Simplify RecommendationService
- Remove optional dependencies: `FeedbackTrackingService`, `ModelRetrainingService`, `CollaborativeFilteringService`, `IUserRatingRepository`
- Keep `RecommendationEngine` for now (still used by `RecommendSimilarCars`)
- Remove `TrackRecommendations()` and `TryTriggerRetraining()` methods

### Phase 4: Verify
- Build solution
- Test all active endpoints
- Verify ML evaluation still works

