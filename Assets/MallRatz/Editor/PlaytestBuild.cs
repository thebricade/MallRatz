using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MallRatz.Editor
{
    // One click to a zip you can send a friend: a Windows build of just the greybox scene.
    public static class PlaytestBuild
    {
        const string OutputFolder = "Builds/Playtest";
        const string ZipPath = "Builds/MallRatz-Playtest.zip";

        [MenuItem("MallRatz/Build Windows Playtest")]
        public static void Build()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/MallRatz/Scenes/Greybox.unity" },
                locationPathName = $"{OutputFolder}/MallRatz.exe",
                target = BuildTarget.StandaloneWindows64,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Playtest build {report.summary.result}. See the console for errors.");
                return;
            }

            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            ZipFile.CreateFromDirectory(OutputFolder, ZipPath);
            EditorUtility.RevealInFinder(ZipPath);
            Debug.Log($"Playtest build ready: {Path.GetFullPath(ZipPath)}");
        }
    }
}
