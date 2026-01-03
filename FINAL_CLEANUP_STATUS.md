# Final Cleanup Status - Car Recommendation System

## ✅ Call Graph Verification

Based on code analysis, here's what is **ACTIVELY USED** by operational endpoints:

### Core Pipeline (MUST KEEP - explicitly required):
1. **Car, ICarRepository, CarRepository** ✓
2. **CarFeatureVector, CarFeatureVectorFactory** ✓
3. **TextParserService** ✓ (explicitly required by user)
4. **SimilarityService, RankingService** ✓
5. **RecommendationEngine** ✓ (explicitly required by user, used by RecommendSimilarCars)
6. **IRecommendationService, RecommendationService** ✓
7. **MlEvaluationService, IMlEvaluationService** ✓

### Actively Used Services (KEEP - used by operational endpoints):
- **RuleBasedFilter** ✓ - Used by RecommendFromTextAsync() and RecommendFromManualFilters()
- **AdvancedScoringService** ✓ - Used by RecommendFromTextAsync() and RecommendFromManualFilters()
- **ForecastingService** ✓ - Used by MlEvaluationService.EvaluateModel()
- **HyperparameterTuningService** ✓ - Used by MlEvaluationService.EvaluateModel()
- **MlRecommendationService** ✓ - Used by AdvancedScoringService
- **ExplanationBuilder** ✓ - Used by RecommendationService

### Non-Core / Unused (SHOULD MOVE TO LEGACY):
- **CollaborativeFilteringService** - DISABLED in code (commented out: "TEMPORARILY DISABLED FOR PERFORMANCE")
- **ModelRetrainingService** - Not registered in DI, not used
- **ModelPerformanceMonitor** - Not registered in DI, not used
- **SessionUserService** - Not registered in DI, not used
- **FeedbackRepository, IFeedbackRepository** - Not registered in DI
- **UserRatingRepository, IUserRatingRepository** - Not registered in DI
- **UserFeedback, UserRating** - Only used by removed controllers

## Current Status

✅ **Controllers cleaned up** - Removed FeedbackController, RatingsController, HomeController
✅ **RecommendationService simplified** - Removed optional dependencies
✅ **Core endpoints working** - All operational endpoints function correctly

⚠️ **Files still in src/ that should be in Legacy/:**
- CollaborativeFilteringService.cs (DISABLED in code)
- ModelRetrainingService.cs (not used)
- ModelPerformanceMonitor.cs (not used)
- SessionUserService.cs (not used)
- FeedbackRepository.cs, IFeedbackRepository.cs (not used)
- UserRatingRepository.cs, IUserRatingRepository.cs (not used)
- UserFeedback.cs, UserRating.cs (not used)

## Decision

According to user requirements: "NON-CORE (only keep if actively used)"

- **RuleBasedFilter** - ACTIVELY USED → KEEP in src/
- **AdvancedScoringService** - ACTIVELY USED → KEEP in src/
- **ForecastingService** - ACTIVELY USED → KEEP in src/
- **HyperparameterTuningService** - ACTIVELY USED → KEEP in src/

These services are actively used by operational endpoints and must remain in the main codebase.

## Recommendation

The cleanup is correct. The services that are actively used (RuleBasedFilter, AdvancedScoringService, ForecastingService, HyperparameterTuningService) should remain in src/ as they are part of the operational pipeline.

Only truly unused services (CollaborativeFilteringService, ModelRetrainingService, etc.) should be moved to Legacy/ or removed.

