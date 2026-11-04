namespace Pagpag.Core
{
    /// <summary>Playable characters. Stored by value in save files — never reorder.</summary>
    public enum Gender
    {
        Male = 0,   // Dong
        Female = 1  // Inday
    }

    /// <summary>
    /// Survival pressure. The game is English-only; "Tagalog" and "Bisaya" are only the
    /// display names of these modes (see <see cref="DifficultySettings"/>).
    /// </summary>
    public enum Difficulty
    {
        Normal = 0, // "Tagalog (Normal)"
        Hard = 1    // "Bisaya (Hard)"
    }

    /// <summary>Four time slots of a day.</summary>
    public enum DayPeriod
    {
        Morning,    // 06:00–12:00
        Afternoon,  // 12:00–18:00
        Evening,    // 18:00–22:00
        LateNight   // 22:00–06:00
    }

    public enum TravelMode
    {
        Walk,
        Jeepney
    }

    /// <summary>The ability a job tests. Children's trait values in these decide how well they do.</summary>
    public enum Skill
    {
        Strength,
        Dexterity,
        Smarts,
        Charm
    }

    /// <summary>How a run ended.</summary>
    public enum EndKind
    {
        None = 0,
        Death = 1,
        Pregnancy = 2,
        /// <summary>Survived the goal with the whole family alive. Unlocks Infinite Mode.</summary>
        Victory = 3,
        /// <summary>Reached the day goal, but someone in the family had died. The run ends without a win.</summary>
        SurvivedWithLosses = 4,
        /// <summary>The best ending: the family left the slums with the money quota saved.</summary>
        LeftTheSlums = 5
    }

    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string CharacterCreation = "CharacterCreation";
        public const string World = "World";
    }
}
