using System.Text.RegularExpressions;
using Pagpag.Localization;

namespace Pagpag.Characters
{
    /// <summary>Player-name validation shared by character creation and save loading.</summary>
    public static class NameRules
    {
        private static readonly Regex Allowed = new Regex(@"^[\p{L}][\p{L} '\-]*$");
        private static readonly Regex Spaces = new Regex(@"\s+");

        public static string Clean(string raw) => Spaces.Replace(raw ?? string.Empty, " ").Trim();

        /// <returns>Localized error message, or null when the cleaned name is valid.</returns>
        public static string Validate(string raw, int minLength, int maxLength, out string clean)
        {
            clean = Clean(raw);
            if (clean.Length < minLength) return Loc.Get("cc.name.err.short", minLength);
            if (clean.Length > maxLength) return Loc.Get("cc.name.err.long", maxLength);
            if (!Allowed.IsMatch(clean)) return Loc.Get("cc.name.err.chars");
            return null;
        }
    }
}
