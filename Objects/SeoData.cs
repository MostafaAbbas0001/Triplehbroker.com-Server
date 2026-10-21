namespace WebsiteAPI.Objects;

public class SeoData
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CanonicalUrl { get; set; }
    public RobotsData Robots { get; set; } = new();
    public OpenGraphData OpenGraph { get; set; } = new();
}
