using System;
using System.IO;
using System.Linq;

// Quick test script om te zien hoeveel auto's een image match hebben
class ImageMatchTest
{
    static void Main()
    {
        string imagesDir = @"C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\backend\images";
        string csvPath = @"C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\backend\data\Cleaned_Car_Data_For_App_Fully_Enriched.csv";
        
        if (!Directory.Exists(imagesDir))
        {
            Console.WriteLine($"Images directory niet gevonden: {imagesDir}");
            return;
        }
        
        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"CSV niet gevonden: {csvPath}");
            return;
        }
        
        // Lees alle image bestanden
        var imageFiles = Directory.GetFiles(imagesDir, "*.jpg");
        Console.WriteLine($"Gevonden {imageFiles.Length} image bestanden");
        
        // Parse eerste paar image namen
        Console.WriteLine("\nEerste 5 image bestandsnamen:");
        foreach (var img in imageFiles.Take(5))
        {
            string fileName = Path.GetFileNameWithoutExtension(img);
            string[] parts = fileName.Split('_');
            Console.WriteLine($"  {fileName}");
            Console.WriteLine($"    -> Brand: {parts[0]}, Model: {(parts.Length > 1 ? parts[1] : "N/A")}");
        }
        
        // Lees CSV en test matching
        Console.WriteLine("\n\nLezen CSV en testen matches...");
        var lines = File.ReadAllLines(csvPath);
        if (lines.Length == 0)
        {
            Console.WriteLine("CSV is leeg");
            return;
        }
        
        string[] headers = lines[0].ToLower().Split(',');
        int brandIndex = Array.IndexOf(headers, "merk");
        int modelIndex = Array.IndexOf(headers, "model");
        
        if (brandIndex < 0 || modelIndex < 0)
        {
            Console.WriteLine($"Brand index: {brandIndex}, Model index: {modelIndex}");
            Console.WriteLine("Headers: " + string.Join(", ", headers.Take(10)));
            return;
        }
        
        int totalCars = 0;
        int matches = 0;
        
        foreach (var line in lines.Skip(1).Take(100)) // Test eerste 100 auto's
        {
            string[] cols = line.Split(',');
            if (cols.Length <= Math.Max(brandIndex, modelIndex))
                continue;
                
            string brand = cols[brandIndex]?.Trim() ?? "";
            string model = cols[modelIndex]?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(model))
                continue;
                
            totalCars++;
            
            // Normaliseer zoals in code
            string brandNorm = Normalize(brand);
            string modelNorm = Normalize(model);
            
            // Zoek match
            bool found = false;
            foreach (var img in imageFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(img);
                string[] parts = fileName.Split('_');
                
                if (parts.Length < 2)
                    continue;
                    
                string imgBrand = Normalize(parts[0]);
                string imgModel = Normalize(parts[1]);
                
                if ((imgBrand == brandNorm || imgBrand.Contains(brandNorm) || brandNorm.Contains(imgBrand)) &&
                    (imgModel == modelNorm || imgModel.Contains(modelNorm) || modelNorm.Contains(imgModel)))
                {
                    found = true;
                    break;
                }
            }
            
            if (found)
                matches++;
        }
        
        Console.WriteLine($"\nResultaten (eerste 100 auto's):");
        Console.WriteLine($"  Totaal auto's getest: {totalCars}");
        Console.WriteLine($"  Matches gevonden: {matches}");
        Console.WriteLine($"  Percentage: {(matches * 100.0 / totalCars):F1}%");
    }
    
    static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
            
        string normalized = name.ToLower().Trim();
        normalized = normalized.Replace(" ", "_").Replace("-", "_");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9_]", "");
        return normalized;
    }
}






