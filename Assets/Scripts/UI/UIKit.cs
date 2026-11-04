using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Runtime UI construction helpers (the editor builders use UIBuilder; panels that are filled
    /// dynamically — family panel, minigames, wheel — build themselves with this).
    /// Positions are relative to the parent's centre unless an anchor is given.
    /// </summary>
    public static class UIKit
    {
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        private static Font _font;

        public static Sprite Rounded;
        public static Sprite Circle;
        public static Sprite Ring;

        public static readonly Color Panel = new Color(0.07f, 0.09f, 0.12f, 1f);
        public static readonly Color PanelLight = new Color(0.12f, 0.15f, 0.2f, 1f);
        public static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.29f, 1f);
        public static readonly Color Gold = new Color(0.85f, 0.64f, 0.25f);
        public static readonly Color Cream = new Color(0.93f, 0.91f, 0.86f);
        public static readonly Color Muted = new Color(0.65f, 0.66f, 0.68f);
        public static readonly Color Danger = new Color(0.92f, 0.38f, 0.35f);
        public static readonly Color Good = new Color(0.5f, 0.85f, 0.4f);
        public static readonly Color Dark = new Color(0.08f, 0.08f, 0.1f);
        public static readonly Color Grief = new Color(0.45f, 0.35f, 0.62f);

        public static readonly Color HungerColor = new Color(0.95f, 0.58f, 0.15f);
        public static readonly Color EnergyColor = new Color(0.92f, 0.78f, 0.25f);
        public static readonly Color HealthColor = new Color(0.85f, 0.2f, 0.2f);

        public static void Init(Sprite rounded, Sprite circle, Sprite ring)
        {
            Rounded = rounded;
            Circle = circle;
            Ring = ring;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2? anchor = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = pivot ?? a;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(string name, Transform parent, float inset = 0)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Img(RectTransform rt, Color color, bool rounded = false, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (rounded && Rounded != null)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }
            return img;
        }

        public static Image CircleImg(RectTransform rt, Color color, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = Circle;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Txt(RectTransform rt, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal, bool fit = false)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true;
            if (fit)
            {
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = Mathf.Max(8, size / 2);
                t.resizeTextMaxSize = size;
            }
            return t;
        }

        public static Text Label(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, Vector2? anchor = null, bool fit = false)
            => Txt(Rect(name, parent, pos, size, anchor), text, fontSize, color, align, style, fit);

        public static Button Btn(string name, Transform parent, Vector2 pos, Vector2 size, string label, Action onClick,
            Color? bg = null, Color? fg = null, int fontSize = 24, Vector2? anchor = null)
        {
            var rt = Rect(name, parent, pos, size, anchor);
            var img = Img(rt, bg ?? ButtonColor, true, true);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            var t = Txt(Fill("Label", rt, 6), label, fontSize, fg ?? Cream, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Text LabelOf(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>A horizontal meter: returns the fill RectTransform (set anchorMax.x to the value).</summary>
        public static RectTransform Bar(string name, Transform parent, Vector2 pos, Vector2 size, Color fill, out Image fillImage,
            Vector2? anchor = null)
        {
            var track = Rect(name, parent, pos, size, anchor);
            Img(track, new Color(0.15f, 0.16f, 0.19f), true);
            var f = Fill("Fill", track);
            f.anchorMax = new Vector2(1, 1);
            fillImage = Img(f, fill, true);
            return f;
        }

        public static void SetBar(RectTransform fill, float value01) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);

        public static void Clear(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>Button that reports being held down (for pour, lean, wring...).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) => Held = true;
        public void OnPointerUp(PointerEventData e) => Held = false;
        public void OnPointerExit(PointerEventData e) => Held = false;
        private void OnDisable() => Held = false;
        /// <summary>For automated tests.</summary>
        public void SetHeld(bool held) => Held = held;
    }
}
