using Pagpag.Localization;
using Pagpag.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pagpag.EditorTools
{
    /// <summary>Small helpers for building uGUI hierarchies from editor code (1920x1080 reference).</summary>
    public static class UIBuilder
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.12f, 1f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.2f, 0.26f, 0.95f);
        public static readonly Color Gold = new Color(0.85f, 0.64f, 0.25f);
        public static readonly Color Cream = new Color(0.92f, 0.9f, 0.85f);
        public static readonly Color Danger = new Color(0.9f, 0.42f, 0.4f);
        public static readonly Color PagpagRed = new Color(0.78f, 0.16f, 0.16f);

        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Anchor presets
        public static readonly Vector2 TopLeft = new Vector2(0, 1), TopCenter = new Vector2(0.5f, 1), TopRight = new Vector2(1, 1);
        public static readonly Vector2 MidLeft = new Vector2(0, 0.5f), Center = new Vector2(0.5f, 0.5f), MidRight = new Vector2(1, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0, 0), BottomCenter = new Vector2(0.5f, 0), BottomRight = new Vector2(1, 0);

        public static Canvas CreateSceneRoot(string canvasName, Color cameraColor)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cameraColor;
            camGo.transform.position = new Vector3(0, 0, -10);

            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject(canvasName, typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<ResponsiveCanvas>();
            return canvas;
        }

        /// <summary>
        /// Container for a scene's UI inside the device safe area. With <paramref name="fitDesign"/>, adds a
        /// 1920×1080 design area that shrinks to fit small screens and stretches on large ones, so screens laid
        /// out for 1920×1080 are never clipped.
        /// </summary>
        public static RectTransform MakeResponsiveRoot(Transform canvas, bool fitDesign)
        {
            var safe = MakeStretch("SafeArea", canvas);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return fitDesign ? FitDesign("Content", safe, new Vector2(1920, 1080), true) : safe;
        }

        /// <summary>A point-anchored region designed at <paramref name="design"/> that scales to fit its parent.</summary>
        public static RectTransform FitDesign(string name, Transform parent, Vector2 design, bool stretch, Vector2? reserved = null)
        {
            var rt = MakeRect(name, parent, Center, Vector2.zero, design);
            var fit = rt.gameObject.AddComponent<FitInParent>();
            fit.designSize = design;
            fit.stretchWidth = stretch;
            fit.stretchHeight = stretch;
            fit.reserved = reserved ?? Vector2.zero;
            return rt;
        }

        /// <summary>Makes an existing point-anchored rect shrink to fit its parent.</summary>
        public static FitInParent AddFit(RectTransform rt, Vector2 design, bool fitHeight = true, bool stretchWidth = false, Vector2? reserved = null)
        {
            var fit = rt.gameObject.AddComponent<FitInParent>();
            fit.designSize = design;
            fit.fitHeight = fitHeight;
            fit.stretchWidth = stretchWidth;
            fit.reserved = reserved ?? Vector2.zero;
            return fit;
        }

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform MakeStretch(string name, Transform parent, float inset = 0)
        {
            var rt = MakeRect(name, parent, Center, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image MakePanel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null)
        {
            // Thin gold frame with an opaque fill inset inside it; children go on the fill.
            var frame = MakeRect(name, parent, anchor, pos, size);
            AddImage(frame, new Color(Gold.r, Gold.g, Gold.b, 0.45f));
            return AddImage(MakeStretch("Fill", frame, 2), color ?? PanelColor);
        }

        /// <summary>Text with an optional localization key. The editor preview shows the current language.</summary>
        public static Text AddText(RectTransform rt, string keyOrLiteral, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool localized = true, bool shadow = false)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true;
            if (localized && !string.IsNullOrEmpty(keyOrLiteral))
            {
                t.text = Loc.Get(keyOrLiteral);
                var lt = rt.gameObject.AddComponent<LocalizedText>();
                SetField(lt, "key", keyOrLiteral);
            }
            else
            {
                t.text = keyOrLiteral ?? string.Empty;
            }
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.85f);
                s.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        public static Text MakeLabel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string keyOrLiteral,
            int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal,
            bool localized = true, bool shadow = false)
            => AddText(MakeRect(name, parent, anchor, pos, size), keyOrLiteral, fontSize, color, align, style, localized, shadow);

        public static Button MakeButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string keyOrLiteral,
            int fontSize = 34, bool localized = true, Color? bg = null, Color? textColor = null)
        {
            var rt = MakeRect(name, parent, anchor, pos, size);
            var img = AddImage(rt, bg ?? ButtonColor, null, true);
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
            outline.effectDistance = new Vector2(2, -2);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.88f, 0.88f, 0.88f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var label = AddText(MakeStretch("Label", rt, 8), keyOrLiteral, fontSize, textColor ?? Cream, TextAnchor.MiddleCenter, FontStyle.Bold, localized);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = fontSize;
            return btn;
        }

        public static OptionToggle MakeOption(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            string keyOrLiteral, int fontSize = 30, bool localized = true)
        {
            var btn = MakeButton(name, parent, anchor, pos, size, keyOrLiteral, fontSize, localized);
            var opt = btn.gameObject.AddComponent<OptionToggle>();
            SetField(opt, "background", btn.targetGraphic);
            SetField(opt, "label", btn.GetComponentInChildren<Text>());
            return opt;
        }

        public static InputField MakeInput(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string placeholderKey)
        {
            var rt = MakeRect(name, parent, anchor, pos, size);
            var bg = AddImage(rt, new Color(0.93f, 0.91f, 0.86f), null, true);
            var field = rt.gameObject.AddComponent<InputField>();
            field.targetGraphic = bg;

            var placeholder = AddText(MakeStretch("Placeholder", rt, 14), placeholderKey, 28, new Color(0.3f, 0.3f, 0.3f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Italic);
            var text = AddText(MakeStretch("Text", rt, 14), "", 30, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleLeft, FontStyle.Bold, false);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.caretColor = Color.black;
            field.caretWidth = 3;
            return field;
        }

        public static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Builder] {target.GetType().Name}.{field} not found"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Builder] {target.GetType().Name}.{field} not found"); return; }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Full-screen background that keeps the image aspect ratio (crops instead of stretching).</summary>
        public static Image MakeBackground(Transform canvas, Sprite sprite, Color tint)
        {
            var rt = MakeStretch("Background", canvas);
            var img = AddImage(rt, tint, sprite);
            if (sprite != null)
            {
                var fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
            return img;
        }
    }
}
