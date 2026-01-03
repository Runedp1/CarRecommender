# Call Graph Analysis - Car Recommendation System

## Core Pipeline Call Graph

### Controllers → Services → Engine → Repository

```
CarsController
  └─> ICarRepository (CarRepository)
      └─> Car (domain model)

RecommendationsController
  ├─> ICarRepository (CarRepository)
  └─> IRecommendationService (RecommendationService)
      ├─> ICarRepository
      ├─> RecommendationEngine (for RecommendSimilarCars)
      ├─> TextParserService (CORE - text parsing)
      ├─> ExplanationBuilder
      ├─> RuleBasedFilter (CORE - used in RecommendFromText/ManualFilters)
      ├─> CarFeatureVectorFactory (CORE)
      ├─> SimilarityService (CORE)
      ├─> RankingService (CORE)
      ├─> MlRecommendationService
      └─> AdvancedScoringService
          ├─> CarFeatureVectorFactory
          ├─> SimilarityService
          ├─> MlRecommendationService
          └─> ICarRepository

MlController
  └─> IMlEvaluationService (MlEvaluationService)
      ├─> ICarRepository
      ├─> IRecommendationService
      ├─> HyperparameterTuningService
      │   └─> ICarRepository
      └─> ForecastingService

HealthController
  └─> ICarRepository (optional)
```

## Core Components (MUST KEEP)

### Domain & Data
- ✅ Car
- ✅ ICarRepository
- ✅ CarRepository

### Feature Engineering & Similarity
- ✅ CarFeatureVector
- ✅ CarFeatureVectorFactory
- ✅ TextParserService (CORE - part of pipeline)
- ✅ SimilarityService
- ✅ RankingService

### Recommendation Logic
- ✅ RecommendationEngine
- ✅ IRecommendationService
- ✅ RecommendationService
- ✅ RuleBasedFilter (used by RecommendationService)
- ✅ AdvancedScoringService (used by RecommendationService)
- ✅ MlRecommendationService (used by AdvancedScoringService)
- ✅ ExplanationBuilder (used by RecommendationService)

### ML Evaluation
- ✅ IMlEvaluationService
- ✅ MlEvaluationService
- ✅ HyperparameterTuningService (used by MlEvaluationService)
- ✅ ForecastingService (used by MlEvaluationService)

### Controllers
- ✅ CarsController
- ✅ RecommendationsController
- ✅ MlController
- ✅ HealthController

## Non-Core Components (Already in Legacy or Can Be Moved)

### Already in src/Legacy/
- ✅ ModelPerformanceMonitor (references non-existent FeedbackTrackingService)
- ✅ ModelRetrainingService (references non-existent FeedbackTrackingService)
- ✅ SessionUserService
- ✅ RetrainingBackgroundService (in src/Legacy/)

### Duplicate Found
- ⚠️ RetrainingBackgroundService exists in TWO locations:
  - src/Legacy/RetrainingBackgroundService.cs
  - backend/CarRecommender.Api/Services/RetrainingBackgroundService.cs
  - **Action**: Remove duplicate from backend (keep in Legacy)

## User Feedback Code (OUT OF SCOPE - Remove if found)
- UserFeedback
- UserRating
- UserRatingRepository
- IFeedbackRepository
- FeedbackRepository
- FeedbackTrackingService (referenced but doesn't exist)
- Feedback/Rating controllers (none found in backend)

## Services Status

| Service | Location | Status | Used By |
|---------|----------|--------|---------|
| AdvancedScoringService | src/ | ✅ CORE | RecommendationService |
| RuleBasedFilter | src/ | ✅ CORE | RecommendationService |
| ForecastingService | src/ | ✅ CORE | MlEvaluationService |
| HyperparameterTuningService | src/ | ✅ CORE | MlEvaluationService |
| MlRecommendationService | src/ | ✅ CORE | AdvancedScoringService |
| ExplanationBuilder | src/ | ✅ CORE | RecommendationService |
| ModelRetrainingService | src/Legacy/ | ⚠️ Legacy | (not used) |
| ModelPerformanceMonitor | src/Legacy/ | ⚠️ Legacy | (not used) |
| SessionUserService | src/Legacy/ | ⚠️ Legacy | (not used) |
| RetrainingBackgroundService | src/Legacy/ | ⚠️ Legacy | (not used) |
| RetrainingBackgroundService | backend/.../Services/ | ❌ Duplicate | (not used) |

## Conclusion

**All core services are correctly placed in src/ folder.**
**Non-core services are already in src/Legacy/.**
**Only cleanup needed: Remove duplicate RetrainingBackgroundService from backend.**
