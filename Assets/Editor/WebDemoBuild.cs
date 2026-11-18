using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// Builds a playable web (WebGL) demo into Builds/WebGL.
    /// Menu: Pagpag ▸ Build Web Demo. Command line:
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod Pagpag.EditorTools.WebDemoBuild.Build
    /// </summary>
    public static class WebDemoBuild
    {
        public const string OutputDir = "Builds/WebGL";

        [MenuItem("Pagpag/Build Web Demo")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebDemoBuild] No scenes in Build Settings. Run Pagpag ▸ Build All first.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // Gzip with a JavaScript fallback: works on any static host (Vercel, GitHub Pages)
            // without special Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.runInBackground = true;

            if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[WebDemoBuild] {report.summary.result}: {report.summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{report.summary.totalErrors} errors, {report.summary.totalTime:mm\\:ss}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
