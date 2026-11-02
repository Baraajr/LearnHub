namespace LearnHub.Services.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15; // Fallback default
    public int RefreshTokenLifetimeDays { get; set; } = 7; // Fallback default
}
