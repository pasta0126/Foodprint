namespace Foodprint.Core.Domain;

/// <summary>A person with access to Foodprint. Created on account activation.</summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Lower-cased, unique.</summary>
    public required string Email { get; set; }

    /// <summary>Null until the account has been activated via a registration link.</summary>
    public string? PasswordHash { get; set; }

    public bool IsAdmin { get; set; }

    /// <summary>Set when an admin disables the account; blocks sign-in and kills sessions.</summary>
    public DateTime? DisabledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Profile Profile { get; set; } = null!;
    public ICollection<Session> Sessions { get; } = new List<Session>();
    public ICollection<MealEntry> MealEntries { get; } = new List<MealEntry>();
    public ICollection<Tag> Tags { get; } = new List<Tag>();
    public ICollection<MealFavorite> Favorites { get; } = new List<MealFavorite>();

    public bool IsDisabled => DisabledAt is not null;
    public bool IsActivated => PasswordHash is not null;
}

/// <summary>Per-user settings: 1:1 with <see cref="User"/>.</summary>
public class Profile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string DisplayName { get; set; }

    /// <summary>IANA time-zone id, e.g. "Europe/Madrid". Authority for all day/week bucketing.</summary>
    public required string TimeZoneId { get; set; }

    /// <summary>UI language: one of the codes in <see cref="Localization.SupportedLanguages"/>.</summary>
    public required string Language { get; set; }
}

/// <summary>
/// A one-time token link that lets a person set their password and (first time) activate
/// their account. Created by the admin CLI or the optional self-register form.
/// </summary>
public class RegistrationLink
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Lower-cased email the link is for.</summary>
    public required string Email { get; set; }

    /// <summary>SHA-256 of the raw token. The raw token is never stored.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool CreatedByAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive(DateTime nowUtc) =>
        RevokedAt is null && UsedAt is null && ExpiresAt > nowUtc;
}

/// <summary>An authenticated browser session, keyed by an opaque cookie token.</summary>
public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 of the raw cookie token.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A member of the closed meal-group catalog (breakfast, lunch, ...). Seeded; managed only via the CLI.</summary>
public class MealGroup
{
    public int Id { get; set; }

    /// <summary>Stable machine key; display name is localized from resources by this key.</summary>
    public required string Key { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Set when retired: kept for existing entries, hidden from the picker.</summary>
    public DateTime? RetiredAt { get; set; }

    public bool IsActive => RetiredAt is null;
}

/// <summary>A single diary entry: something a user ate at a point in time.</summary>
public class MealEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>Instant the meal was eaten, stored UTC (server clock).</summary>
    public DateTime EatenAt { get; set; }

    /// <summary>
    /// Named portion size (small/medium/large/very-large) XOR <see cref="PortionGrams"/>.
    /// New and edited entries require exactly one of the two; older rows may have neither.
    /// </summary>
    public string? PortionSize { get; set; }

    /// <summary>Portion in grams (1..5000) XOR <see cref="PortionSize"/>.</summary>
    public int? PortionGrams { get; set; }

    public int? MealGroupId { get; set; }
    public MealGroup? MealGroup { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MealEntryTag> EntryTags { get; } = new List<MealEntryTag>();
}

/// <summary>
/// A saved meal template scoped to one user: the reusable parts of an entry
/// (name, portion, meal group, tags, notes) with no time. Surfaced as quick-add
/// cards. Identified by user + normalized name + meal group.
/// </summary>
public class MealFavorite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>Trimmed, lower-cased <see cref="Name"/>. The dedup key together with <see cref="MealGroupId"/>.</summary>
    public required string NameNormalized { get; set; }

    /// <summary>Named portion size XOR <see cref="PortionGrams"/>. May be null on a bare template.</summary>
    public string? PortionSize { get; set; }

    public int? PortionGrams { get; set; }

    public int? MealGroupId { get; set; }
    public MealGroup? MealGroup { get; set; }

    /// <summary>Normalized tags joined by ", ". Empty string when none.</summary>
    public string TagsCsv { get; set; } = "";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A free-form label scoped to one user. Normalized to trimmed lower-case.</summary>
public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public ICollection<MealEntryTag> EntryTags { get; } = new List<MealEntryTag>();
}

/// <summary>Join row between <see cref="MealEntry"/> and <see cref="Tag"/>.</summary>
public class MealEntryTag
{
    public Guid MealEntryId { get; set; }
    public MealEntry MealEntry { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
