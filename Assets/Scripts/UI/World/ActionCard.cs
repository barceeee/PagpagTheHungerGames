using System;
using Pagpag.Locations;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>One action at a location: what it costs, what it does, and a button to do it.</summary>
    public class ActionCard : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button doButton;
        [SerializeField] private Text doLabel;
        [SerializeField] private CanvasGroup group;

        public void Bind(LocationAction action, string details, string blockedReason, Action onClick)
        {
            nameText.text = action.name;
            detailsText.text = details;
            var blocked = blockedReason != null;
            descriptionText.text = blocked ? blockedReason : action.description;
            descriptionText.color = blocked ? new Color(1f, 0.6f, 0.55f) : new Color(0.88f, 0.86f, 0.81f);
            doLabel.text = Verb(action.kind);
            doButton.interactable = !blocked;
            group.alpha = 1f; // stays fully readable; the button itself shows the disabled state
            doButton.onClick.RemoveAllListeners();
            doButton.onClick.AddListener(() => onClick());
        }

        private static string Verb(ActionKind kind) => kind switch
        {
            ActionKind.Eat => "Eat",
            ActionKind.Work => "Work",
            ActionKind.Sleep => "Sleep",
            ActionKind.Talk => "Talk",
            ActionKind.Provision => "Buy",
            ActionKind.FamilyMeal => "Serve",
            ActionKind.PantryMeal => "Eat",
            ActionKind.Intimacy => "Together",
            ActionKind.LeaveSlums => "Leave",
            ActionKind.BuyMedicine => "Buy",
            ActionKind.TakeMedicine => "Take",
            ActionKind.Consult => "Ask",
            _ => "Do it"
        };
    }
}
