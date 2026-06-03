namespace RandomSkillsPlugin;

/// <summary>Metadata for a skill shown to the player.</summary>
public sealed class SkillInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    /// <summary>True if the skill is triggered by the E key (active), else passive.</summary>
    public bool IsActive { get; init; }
}

public static class Skills
{
    public static readonly Dictionary<string, SkillInfo> All = new()
    {
        ["speed"] = new() { Id = "speed", Name = "⚡ Prędkość", Description = "Poruszasz się szybciej." },
        ["tank"] = new() { Id = "tank", Name = "🛡️ Tank", Description = "Dużo HP, ale wolniej się poruszasz." },
        ["health"] = new() { Id = "health", Name = "❤️ Dodatkowe HP", Description = "Zaczynasz rundę z większym zdrowiem." },
        ["damage"] = new() { Id = "damage", Name = "💥 Większe obrażenia", Description = "Zadajesz więcej obrażeń." },
        ["teleport"] = new() { Id = "teleport", Name = "🌀 Teleport (E)", Description = "Naciśnij E, aby teleportować się w miejsce celownika.", IsActive = true },
        ["lowgrav"] = new() { Id = "lowgrav", Name = "🪶 Niska grawitacja", Description = "Skaczesz wyżej." },
        ["invis"] = new() { Id = "invis", Name = "👻 Niewidzialność", Description = "Jesteś częściowo niewidzialny." }
    };
}
