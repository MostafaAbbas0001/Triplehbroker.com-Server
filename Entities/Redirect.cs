namespace WebsiteAPI.Entities;

public class Redirect
{
    public Guid Id { get; set; }
    public string OldPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
