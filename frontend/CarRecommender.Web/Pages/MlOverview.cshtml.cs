using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarRecommender.Web.Pages;

public class MlOverviewModel : PageModel
{
    private readonly IConfiguration _configuration;
    
    public string ApiBaseUrl { get; set; } = string.Empty;
    
    public MlOverviewModel(IConfiguration configuration)
    {
        _configuration = configuration;
        ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty;
    }
    
    public void OnGet()
    {
        // Page initialization
    }
}