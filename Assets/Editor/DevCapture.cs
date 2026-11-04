using System.IO;
using UnityEngine;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// Developer helper: renders the current scene's UI to a PNG even when the Game view
    /// is not repainting (used for automated visual checks).
    /// </summary>
    public static class DevCapture
    {
        public static void Capture(string path, int width = 1920, int height = 1080)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f;
            }

            var rt = new RenderTexture(width, height, 24);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            // Lay the UI out for the capture size (responsive components follow the camera's pixel size).
            Pagpag.UI.ResponsiveCanvas.RefreshAll();
            foreach (var d in Object.FindObjectsByType<Pagpag.UI.SideDrawer>()) d.Snap();
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Pagpag.UI.ResponsiveCanvas.RefreshAll();
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;

            for (var i = 0; i < canvases.Length; i++)
                if (canvases[i].isRootCanvas) canvases[i].renderMode = modes[i];
            Pagpag.UI.ResponsiveCanvas.RefreshAll();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
