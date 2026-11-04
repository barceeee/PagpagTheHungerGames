using System;
using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Locations
{
    public enum ActionKind
    {
        Eat,
        Work,
        Sleep,
        Talk,
        Service,
        /// <summary>Buy food for the household pantry.</summary>
        Provision,
        /// <summary>Feed everyone at home from the pantry.</summary>
        FamilyMeal,
        /// <summary>Player eats one pantry serving.</summary>
        PantryMeal,
        /// <summary>Private time with the spouse (fade to black) followed by the fertility wheel.</summary>
        Intimacy,
        /// <summary>Leave the slums for good (Infinite Mode only, needs the money quota). Ends the run.</summary>
        LeaveSlums,
        /// <summary>Buy medicine for the cabinet at home (<see cref="LocationAction.servings"/> doses, or one instant cure).</summary>
        BuyMedicine,
        /// <summary>The player takes medicine from the cabinet (a dose, or the instant cure).</summary>
        TakeMedicine,
        /// <summary>Ask the pharmacist: who in the family is sick and how much medicine they need.</summary>
        Consult
    }

    /// <summary>
    /// One thing the player can do at a location (buy a meal, take a shift, sleep, talk...).
    /// Stats use 100 = best: positive hunger/energy values restore, negative values drain.
    /// Drains are scaled by the difficulty mode; restores are not.
    /// Work only pays through its minigame (<see cref="minigameId"/>).
    /// </summary>
    [Serializable]
    public class LocationAction
    {
        public string id;
        public string name;
        [TextArea] public string description;
        public ActionKind kind;

        [Header("Economy")]
        [Min(0)] public int cost;
        [Tooltip("Pay at full performance (minigame score of 80%). Up to 125% for a perfect game.")]
        [Min(0)] public int wage;

        [Header("Time and effects (100 = best; negative = drain)")]
        [Min(0)] public int durationMinutes = 30;
        public float hunger;
        public float energy;
        public float health;

        [Header("Work")]
        [Tooltip("Minigame played to do this job.")]
        public string minigameId;
        [Tooltip("Ability the job tests (children's skill in it changes performance and pay).")]
        public Skill skill;
        [Tooltip("Youngest child age allowed to take this job. 0 = adults only.")]
        public int childMinAge;

        [Header("Household food")]
        [Tooltip("Servings added to the pantry (Provision).")]
        public int servings;
        [Tooltip("Pantry servings are pagpag (can make people sick).")]
        public bool pagpagServings;

        [Header("Availability")]
        [Tooltip("Character id that can see this action (\"dong\" or \"inday\"). Empty = both. Dong and Inday have different jobs and routines.")]
        public string character;
        [Tooltip("Can only be started between these hours (0 and 24 = any time). May wrap past midnight.")]
        [Range(0, 24)] public int startHourFrom;
        [Range(0, 24)] public int startHourTo = 24;
        public bool oncePerDay;
        public bool requiresSpouse;
        public string requiresFlag;
        [Tooltip("Hidden once this flag is set (one-time actions).")]
        public string hiddenIfFlag;
        public string setsFlag;

        [Header("Risk")]
        [Range(0, 1)] public float riskChance;
        public float riskHealth;
        public float riskEnergy;
        [Tooltip("Shown as the cause of death if this risk kills someone.")]
        public string riskCause = "Injury";
        [TextArea] public string riskMessage;

        [Header("Text")]
        [TextArea] public string resultMessage;
        [Tooltip("If the player has this flag (e.g. knows this NPC), contactMessage replaces resultMessage. Rewards are unchanged.")]
        public string contactFlag;
        [TextArea] public string contactMessage;

        [Header("Illness and medicine")]
        [Tooltip("Pagpag food: eaten day after day it can cause typhoid (on top of the food-poisoning risk).")]
        public bool pagpagFood;
        [Tooltip("BuyMedicine / TakeMedicine: the instant cure instead of a regular dose.")]
        public bool instantCure;

        [Header("Dialog (shown before the action; empty = no dialog)")]
        [Tooltip("Who the player talks to.")]
        public string speaker;
        [Tooltip("What the player says. {name} = player, {spouse} = spouse.")]
        [TextArea] public string playerLine;
        [Tooltip("The reply; one is picked at random each time.")]
        [TextArea] public string[] npcLines;

        public bool HasDialog => npcLines != null && npcLines.Length > 0;
        public bool IsWork => kind == ActionKind.Work;
        public bool IsFood => kind == ActionKind.Eat || kind == ActionKind.Provision;
        public bool ChildrenCanDo => IsWork && childMinAge > 0;
    }
}
