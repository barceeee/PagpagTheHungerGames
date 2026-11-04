using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// Generates simple original placeholder sprites so every phase is playable before final art exists.
    /// Replace the PNGs with illustrated art at any time; references survive because the paths stay the same.
    /// </summary>
    public static class PlaceholderArt
    {
        public const string CharacterDir = "Assets/Sprites/Characters";
        public const string UiDir = "Assets/Sprites/UI";
        public const string RoundedSprite = UiDir + "/Rounded.png";
        public const string CircleSprite = UiDir + "/Circle.png";
        public const string RingSprite = UiDir + "/Ring.png";

        private class Painter
        {
            public readonly int W, H;
            private readonly Color[] _px;

            public Painter(int w, int h)
            {
                W = w; H = h;
                _px = new Color[w * h];
            }

            public void Blend(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                var i = y * W + x;
                var d = _px[i];
                var a = c.a + d.a * (1 - c.a);
                if (a <= 0) return;
                _px[i] = new Color(
                    (c.r * c.a + d.r * d.a * (1 - c.a)) / a,
                    (c.g * c.a + d.g * d.a * (1 - c.a)) / a,
                    (c.b * c.a + d.b * d.a * (1 - c.a)) / a, a);
            }

            public Color Get(int x, int y) => _px[y * W + x];

            public void RoundRect(int x0, int y0, int x1, int y1, int r, Color c)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                {
                    var dx = Mathf.Max(x0 + r - x, 0, x - (x1 - r));
                    var dy = Mathf.Max(y0 + r - y, 0, y - (y1 - r));
                    if (dx * dx + dy * dy <= r * r) Blend(x, y, c);
                }
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c, float minY = float.MinValue)
            {
                for (var y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (var x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    if (y < minY) continue;
                    var nx = (x - cx) / rx;
                    var ny = (y - cy) / ry;
                    if (nx * nx + ny * ny <= 1f) Blend(x, y, c);
                }
            }

            /// <summary>Dark cartoon outline around every opaque shape.</summary>
            public void Outline(Color c, int thickness = 3)
            {
                var copy = (Color[])_px.Clone();
                for (var y = 0; y < H; y++)
                for (var x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 0.5f) continue;
                    var near = false;
                    for (var oy = -thickness; oy <= thickness && !near; oy++)
                    for (var ox = -thickness; ox <= thickness && !near; ox++)
                    {
                        int sx = x + ox, sy = y + oy;
                        if (sx < 0 || sy < 0 || sx >= W || sy >= H || ox * ox + oy * oy > thickness * thickness) continue;
                        near = copy[sy * W + sx].a > 0.5f;
                    }
                    if (near) _px[y * W + x] = c;
                }
            }

            public Texture2D ToTexture()
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
                t.SetPixels(_px);
                t.Apply();
                return t;
            }
        }

        private static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = a;
            return c;
        }

        public static void GenerateAll()
        {
            Directory.CreateDirectory(CharacterDir);
            Directory.CreateDirectory(UiDir);

            var dong = DrawCharacter(false);
            var inday = DrawCharacter(true);
            Write(Portrait(dong, Hex("#2F4858"), Hex("#16222B")), CharacterDir + "/Dong_Portrait.png");
            Write(Portrait(inday, Hex("#6B3A3A"), Hex("#2A1717")), CharacterDir + "/Inday_Portrait.png");
            Write(dong, CharacterDir + "/Dong_Body.png");
            Write(inday, CharacterDir + "/Inday_Body.png");
            Write(HorizontalGradient(), UiDir + "/GradientLeft.png");
            Write(Disc(64, 18, 0f), RoundedSprite);
            Write(Disc(256, 127, 0f), CircleSprite);
            Write(Disc(256, 127, 108f), RingSprite);
            foreach (var kid in Kids)
                Write(DrawKid(kid), KidPortraitPath(kid.id));
            AssetDatabase.Refresh();

            foreach (var path in new[]
                     {
                         CharacterDir + "/Dong_Body.png", CharacterDir + "/Inday_Body.png",
                         CharacterDir + "/Dong_Portrait.png", CharacterDir + "/Inday_Portrait.png",
                         UiDir + "/GradientLeft.png", "Assets/Sprites/Map/MapArt.png"
                     })
                ConfigureSprite(path);
            ConfigureSprite(RoundedSprite, 2048, new Vector4(20, 20, 20, 20));
            ConfigureSprite(CircleSprite);
            ConfigureSprite(RingSprite);
            foreach (var kid in Kids) ConfigureSprite(KidPortraitPath(kid.id));
        }

        // ---------- Children ----------

        public struct KidLook
        {
            public string id;
            public string shirt, hair, skin, bgTop, bgBottom;
            public bool longHair, glasses, cap, clip, pigtails, towel;
            public float headScale;
        }

        public static readonly KidLook[] Kids =
        {
            new KidLook { id = "ed", shirt = "#4E6E5D", hair = "#17110D", skin = "#A26A45", bgTop = "#2F4858", bgBottom = "#16222B", towel = true, headScale = 0.9f },
            new KidLook { id = "edd", shirt = "#E8E4D8", hair = "#1A1410", skin = "#A87050", bgTop = "#33415C", bgBottom = "#151B28", glasses = true, headScale = 0.95f },
            new KidLook { id = "eddie", shirt = "#D9822B", hair = "#1A1410", skin = "#A26A45", bgTop = "#5C4033", bgBottom = "#24170F", cap = true, headScale = 1f },
            new KidLook { id = "joy", shirt = "#8E6BB0", hair = "#1E1612", skin = "#B57B55", bgTop = "#4A3B5C", bgBottom = "#1E1828", longHair = true, clip = true, headScale = 1.05f },
            new KidLook { id = "pia", shirt = "#E58FA6", hair = "#1E1612", skin = "#B9805A", bgTop = "#6B3A4A", bgBottom = "#2A1720", pigtails = true, headScale = 1.12f },
        };

        public static string KidPortraitPath(string id) => $"{CharacterDir}/Kid_{id}_Portrait.png";

        /// <summary>Head-and-shoulders portrait of a child, 256x256.</summary>
        private static Texture2D DrawKid(KidLook k)
        {
            var p = new Painter(256, 256);
            // Background gradient
            for (var y = 0; y < 256; y++)
            {
                var c = Color.Lerp(Hex(k.bgBottom), Hex(k.bgTop), y / 255f);
                p.RoundRect(0, y, 255, y, 0, c);
            }
            var skin = Hex(k.skin);
            var hair = Hex(k.hair);
            var s = k.headScale;
            var cx = 128f;
            var cy = 130f;
            var rx = 62f * s;
            var ry = 70f * s;

            if (k.longHair) p.RoundRect((int)(cx - rx - 8), 30, (int)(cx + rx + 8), (int)(cy + 40), 30, hair);
            if (k.pigtails)
            {
                p.Ellipse(cx - rx - 14, cy - 10, 20, 34, hair);
                p.Ellipse(cx + rx + 14, cy - 10, 20, 34, hair);
            }
            // Shoulders and shirt
            p.RoundRect(28, -20, 228, 52, 36, Hex(k.shirt));
            p.RoundRect(150, -20, 228, 52, 30, new Color(0, 0, 0, 0.12f));
            if (k.towel) p.RoundRect(150, 0, 184, 64, 10, Hex("#E6E0CE"));
            // Neck, ears, head
            p.RoundRect(108, 40, 148, 70, 10, skin);
            p.Ellipse(cx - rx + 2, cy - 4, 12, 16, skin);
            p.Ellipse(cx + rx - 2, cy - 4, 12, 16, skin);
            p.Ellipse(cx, cy, rx, ry, skin);
            // Hair on top
            p.Ellipse(cx, cy + 28 * s, rx + 4, ry * 0.75f, hair, cy + 22 * s);
            if (k.cap)
            {
                p.Ellipse(cx, cy + 40 * s, rx + 6, 46 * s, Hex("#C0392B"), cy + 36 * s);
                p.RoundRect((int)(cx - 10), (int)(cy + 34 * s), (int)(cx + rx + 40), (int)(cy + 46 * s), 6, Hex("#A93226"));
            }
            if (k.clip) p.Ellipse(cx + rx * 0.6f, cy + 46 * s, 9, 9, Hex("#E8B53A"));
            // Face
            var eye = Hex("#1A120E");
            var ey = cy - 2;
            p.Ellipse(cx - 24 * s, ey, 6, 7.5f, eye);
            p.Ellipse(cx + 24 * s, ey, 6, 7.5f, eye);
            p.Ellipse(cx - 23 * s, ey + 2, 2, 2, Color.white);
            p.Ellipse(cx + 25 * s, ey + 2, 2, 2, Color.white);
            if (k.glasses)
            {
                var frame = Hex("#2B2B2B");
                foreach (var gx in new[] { cx - 24 * s, cx + 24 * s })
                {
                    p.Ellipse(gx, ey, 17, 15, frame);
                    p.Ellipse(gx, ey, 13, 11, new Color(0.85f, 0.9f, 1f, 0.35f));
                    p.Ellipse(gx, ey, 6, 7.5f, eye);
                }
                p.RoundRect((int)(cx - 8), (int)ey, (int)(cx + 8), (int)ey + 3, 1, frame);
            }
            p.RoundRect((int)(cx - 10), (int)(cy - 34 * s), (int)(cx + 10), (int)(cy - 30 * s), 2, Hex("#7A3B30"));
            p.Ellipse(cx - 38 * s, cy - 22 * s, 9, 6, new Color(0.9f, 0.4f, 0.4f, 0.25f));
            p.Ellipse(cx + 38 * s, cy - 22 * s, 9, 6, new Color(0.9f, 0.4f, 0.4f, 0.25f));
            return p.ToTexture();
        }

        /// <summary>Anti-aliased rounded square (radius &lt; size/2) or circle; innerRadius &gt; 0 makes a ring.</summary>
        private static Texture2D Disc(int size, int radius, float innerRadius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var half = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                // Distance to a rounded rectangle whose corners have the given radius.
                var dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius - 1), 0);
                var dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius - 1), 0);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01(radius - d);
                if (innerRadius > 0) a *= Mathf.Clamp01(d - innerRadius + 1);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return tex;
        }

        public static void ConfigureSprite(string path, int maxSize = 2048, Vector4 border = default)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = maxSize;
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }

        public static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Write(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // 256x512, y grows upward.
        private static Texture2D DrawCharacter(bool female)
        {
            var p = new Painter(256, 512);
            var skin = female ? Hex("#B57B55") : Hex("#A26A45");
            var hair = female ? Hex("#1E1612") : Hex("#17110D");
            var shirt = female ? Hex("#B9503C") : Hex("#5E7D8C");
            var pants = female ? Hex("#4B3F5C") : Hex("#3B4D6D");
            var shoe = female ? Hex("#6B4B3A") : Hex("#2B2B2B");
            var shade = new Color(0, 0, 0, 0.16f);

            if (female) p.RoundRect(80, 285, 176, 440, 34, hair); // long hair behind

            // Legs and feet
            if (female)
            {
                p.RoundRect(92, 20, 122, 60, 8, skin);
                p.RoundRect(134, 20, 164, 60, 8, skin);
                p.RoundRect(88, 50, 124, 200, 10, pants);
                p.RoundRect(132, 50, 168, 200, 10, pants);
            }
            else
            {
                p.RoundRect(88, 20, 124, 200, 10, pants);
                p.RoundRect(132, 20, 168, 200, 10, pants);
            }
            p.RoundRect(82, 6, 126, 26, 8, shoe);
            p.RoundRect(130, 6, 174, 26, 8, shoe);
            p.RoundRect(84, 168, 172, 218, 14, pants);
            p.RoundRect(150, 30, 168, 210, 6, shade);

            // Arms
            p.RoundRect(48, 214, 76, 330, 13, skin);
            p.RoundRect(180, 214, 208, 330, 13, skin);
            p.Ellipse(62, 210, 14, 14, skin);
            p.Ellipse(194, 210, 14, 14, skin);

            // Torso + sleeves
            p.RoundRect(72, 202, 184, 346, 22, shirt);
            p.RoundRect(44, 284, 82, 346, 14, shirt);
            p.RoundRect(174, 284, 212, 346, 14, shirt);
            p.RoundRect(152, 206, 184, 342, 16, shade);
            if (female)
            {
                p.RoundRect(110, 300, 146, 346, 14, Hex("#E7D9C3")); // collar
            }
            else
            {
                p.RoundRect(148, 290, 176, 352, 8, Hex("#E6E0CE")); // towel over shoulder
                p.RoundRect(96, 230, 140, 246, 4, new Color(0, 0, 0, 0.12f)); // worn print
            }

            // Neck, ears, head
            p.RoundRect(113, 336, 143, 366, 8, skin);
            p.Ellipse(83, 402, 9, 12, skin);
            p.Ellipse(173, 402, 9, 12, skin);
            p.Ellipse(128, 404, 46, 54, skin);
            p.Ellipse(150, 396, 20, 40, new Color(0, 0, 0, 0.07f));

            // Hair (front)
            p.Ellipse(128, 424, 50, 42, hair, 416);
            if (female)
            {
                p.Ellipse(104, 430, 26, 22, hair);
                p.Ellipse(158, 438, 9, 9, Hex("#E8B53A")); // clip
            }
            else
            {
                p.Ellipse(108, 448, 16, 16, hair);
                p.Ellipse(136, 452, 18, 16, hair);
            }

            // Face
            var eye = Hex("#1A120E");
            p.Ellipse(110, 400, 5, female ? 7 : 6, eye);
            p.Ellipse(146, 400, 5, female ? 7 : 6, eye);
            p.Ellipse(111, 402, 1.6f, 1.6f, Color.white);
            p.Ellipse(147, 402, 1.6f, 1.6f, Color.white);
            p.RoundRect(100, 414, 119, 418, 2, hair);
            p.RoundRect(137, 414, 156, 418, 2, hair);
            p.RoundRect(125, 380, 131, 392, 2, new Color(0, 0, 0, 0.12f));
            p.RoundRect(117, 368, 139, 372, 2, female ? Hex("#8A3A34") : Hex("#6B3A2E"));

            p.Outline(Hex("#120D0B"), 3);
            return p.ToTexture();
        }

        private static Texture2D Portrait(Texture2D body, Color top, Color bottom)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Source window around the head and shoulders.
            const float sx0 = 44, sy0 = 290, span = 168;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var bg = Color.Lerp(bottom, top, y / (float)size);
                var dx = (x - size * 0.5f) / size;
                var dy = (y - size * 0.62f) / size;
                bg = Color.Lerp(bg, bg * 1.35f, Mathf.Clamp01(0.35f - (dx * dx + dy * dy)) * 1.5f);
                bg.a = 1;
                var c = body.GetPixelBilinear((sx0 + x / (float)size * span) / body.width,
                                              (sy0 + y / (float)size * span) / body.height);
                tex.SetPixel(x, y, Color.Lerp(bg, new Color(c.r, c.g, c.b, 1), c.a));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D HorizontalGradient()
        {
            var tex = new Texture2D(512, 4, TextureFormat.RGBA32, false);
            for (var x = 0; x < 512; x++)
            {
                var a = Mathf.SmoothStep(0.92f, 0f, x / 511f);
                for (var y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(0.03f, 0.04f, 0.06f, a));
            }
            tex.Apply();
            return tex;
        }
    }
}
