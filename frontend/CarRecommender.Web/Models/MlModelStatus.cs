namespace CarRecommender.Web.Models;

/// <summary>
/// Model voor ML model training status.
/// Komt overeen met CarRecommender.Api.Controllers.MlModelStatus uit de backend.
/// </summary>
public class MlModelStatus
{
    /// <summary>
    /// Of het model getraind is.
    /// </summary>
    public bool IsTrained { get; set; }
    
    /// <summary>
    /// Wanneer het model voor het laatst getraind is.
    /// </summary>
    public DateTime LastTrainingTime { get; set; }
    
    /// <summary>
    /// Aantal training samples gebruikt voor training.
    /// </summary>
    public int TrainingDataCount { get; set; }
    
    /// <summary>
    /// Of het model opgeslagen is op disk.
    /// </summary>
    public bool ModelExists { get; set; }
    
    /// <summary>
    /// Pad waar het model is opgeslagen (indien beschikbaar).
    /// </summary>
    public string? ModelPath { get; set; }
}



