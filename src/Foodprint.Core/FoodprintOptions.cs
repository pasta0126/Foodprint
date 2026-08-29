namespace Foodprint.Core;

/// <summary>Bound from the <c>Foodprint</c> configuration section.</summary>
public class FoodprintOptions
{
    public const string SectionName = "Foodprint";

    /// <summary>The account treated as administrator; ensured on startup.</summary>
    public string AdminEmail { get; set; } = "";

    /// <summary>Absolute base URL used to build activation links, e.g. https://foodprint.northernarchive.com.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>When true, the public /register form can mint activation links.</summary>
    public bool AllowSelfRegistration { get; set; }

    /// <summary>Default IANA time zone for new profiles.</summary>
    public string DefaultTimeZone { get; set; } = "Europe/Madrid";

    /// <summary>Directory where ASP.NET Data Protection keys are persisted.</summary>
    public string DataProtectionKeyPath { get; set; } = "dp-keys";

    public int RegistrationLinkExpiryDays { get; set; } = 30;
}
