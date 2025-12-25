namespace CarRecommender.Web.Models;

/// <summary>
/// Request model voor manuele filters (zonder tekst parsing).
/// Gebruiker geeft expliciet alle voorkeuren op via formulier.
/// 
/// VERSCHIL MET TEKST MODUS:
/// - Tekst modus: TextParserService parseert vrije tekst naar UserPreferences
/// - Manuele modus: Gebruiker geeft direct alle waarden op, geen parsing nodig
/// - Alle velden zijn optioneel (null = geen filter)
/// - Geen km-stand ondersteund (zoals gevraagd)
/// </summary>
public class ManualFilterRequest
{
    /// <summary>Minimum prijs in euro's (optioneel)</summary>
    public decimal? MinPrice { get; set; }
    
    /// <summary>Maximum prijs in euro's (optioneel)</summary>
    public decimal? MaxPrice { get; set; }
    
    /// <summary>Voorkeur merk (optioneel, bijv. "bmw", "audi")</summary>
    public string? Brand { get; set; }
    
    /// <summary>Voorkeur model (optioneel, bijv. "x5", "a4")</summary>
    public string? Model { get; set; }
    
    /// <summary>Voorkeur brandstof (optioneel: "petrol", "diesel", "hybrid", "electric")</summary>
    public string? Fuel { get; set; }
    
    /// <summary>Voorkeur transmissie (optioneel: true = automaat, false = schakel, null = geen voorkeur)</summary>
    public bool? Transmission { get; set; }
    
    /// <summary>Voorkeur carrosserie (optioneel: "suv", "sedan", "hatchback", "station", etc.)</summary>
    public string? BodyType { get; set; }
    
    /// <summary>Minimum bouwjaar (optioneel)</summary>
    public int? MinYear { get; set; }
    
    /// <summary>Maximum bouwjaar (optioneel)</summary>
    public int? MaxYear { get; set; }
    
    /// <summary>Minimum vermogen in KW (optioneel)</summary>
    public int? MinPower { get; set; }
    
    /// <summary>Aantal recommendations om terug te geven (optioneel, standaard 5)</summary>
    public int? Top { get; set; }
    
    // NOTE: km-stand wordt NIET ondersteund in deze versie
    // Dit veld is opzettelijk weggelaten zoals gevraagd door de gebruiker
}


