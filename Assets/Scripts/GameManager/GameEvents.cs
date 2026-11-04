using System;
using Pagpag.Minigames;

namespace Pagpag.Core
{
    /// <summary>
    /// Central event hub. Systems publish here instead of referencing each other directly,
    /// so UI, survival, time, minigames and quests stay decoupled.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<RunData> RunStarted;
        public static event Action<RunData> RunLoaded;
        /// <summary>Any change to time, stats, money, location, family or flags.</summary>
        public static event Action<RunData> RunChanged;
        /// <summary>Feedback for the player. important = warnings and failures.</summary>
        public static event Action<string, bool> MessagePosted;
        /// <summary>The run ended permanently (death or pregnancy).</summary>
        public static event Action<RunData> RunEnded;
        /// <summary>A job wants its minigame played (handled by the World scene's MinigameHost).</summary>
        public static event Action<MinigameRequest> MinigameRequested;
        /// <summary>The intimacy event wants the fade + fertility wheel (handled by WheelOverlay).</summary>
        public static event Action<WheelRequest> WheelRequested;
        /// <summary>Open the family panel on a member (null = first member).</summary>
        public static event Action<string> FamilyPanelRequested;

        public static void RaiseRunStarted(RunData run) => RunStarted?.Invoke(run);
        public static void RaiseRunLoaded(RunData run) => RunLoaded?.Invoke(run);
        public static void RaiseRunChanged(RunData run) => RunChanged?.Invoke(run);
        public static void PostMessage(string text, bool important = false) => MessagePosted?.Invoke(text, important);
        public static void RaiseRunEnded(RunData run) => RunEnded?.Invoke(run);
        public static void OpenFamilyPanel(string memberId = null) => FamilyPanelRequested?.Invoke(memberId);

        public static bool RequestMinigame(MinigameRequest request)
        {
            if (MinigameRequested == null) return false;
            MinigameRequested.Invoke(request);
            return true;
        }

        public static bool RequestWheel(WheelRequest request)
        {
            if (WheelRequested == null) return false;
            WheelRequested.Invoke(request);
            return true;
        }
    }
}
