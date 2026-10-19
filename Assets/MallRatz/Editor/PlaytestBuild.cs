using System;
using System.IO;
using System.IO.Compression;
using MallRatz.Net;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MallRatz.Editor
{
    // One click to a zip you can send a friend: a Windows build of just the greybox scene, stamped with
    // the commit it came from so the host can turn away old copies.
    public static class PlaytestBuild
    {
        const string OutputFolder = "Builds/Playtest";
        const string StampFolder = "Assets/MallRatz/Resources";
        const string StampPath = StampFolder + "/" + BuildStamp.ResourceName + ".txt";

        [MenuItem("MallRatz/Build Windows Playtest")]
        public static void Build()
        {
            string commit = BuildStamp.ReadGit();
            if (commit.EndsWith("+")) Debug.LogWarning("Building with uncommitted changes; the stamp will say so (+).");

            // The stamp only exists for the length of the build, so it never shows up as a change.
            bool madeFolder = !AssetDatabase.IsValidFolder(StampFolder);
            if (madeFolder) AssetDatabase.CreateFolder("Assets/MallRatz", "Resources");
            File.WriteAllText(StampPath, $"{commit}\n{DateTime.Now:MMM d HH:mm}");
            AssetDatabase.ImportAsset(StampPath);

            BuildReport report;
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/MallRatz/Scenes/Greybox.unity" },
                    locationPathName = $"{OutputFolder}/MallRatz.exe",
                    target = BuildTarget.StandaloneWindows64,
                };
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                AssetDatabase.DeleteAsset(madeFolder ? StampFolder : StampPath);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Playtest build {report.summary.result}. See the console for errors.");
                return;
            }

            string zipPath = $"Builds/MallRatz-Playtest-{commit.TrimEnd('+')}{(commit.EndsWith("+") ? "-dirty" : "")}.zip";
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(OutputFolder, zipPath);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(zipPath);
            Debug.Log($"Playtest build ready: {Path.GetFullPath(zipPath)} (build {commit})");
        }
    }
}
