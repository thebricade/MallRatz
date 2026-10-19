using System;
using UnityEngine;

namespace MallRatz.Net
{
    // Which commit this copy of the game was built from, so the host can turn away anyone on an old zip.
    // A trailing + means the game's files had uncommitted changes. Builds read the stamp PlaytestBuild
    // writes into Resources; the editor asks git directly.
    public static class BuildStamp
    {
        public const string ResourceName = "BuildStamp";

        public static string Commit { get; }
        public static string BuiltAt { get; }
        public static bool IsEditor => Application.isEditor;

        static BuildStamp()
        {
#if UNITY_EDITOR
            Commit = ReadGit();
            BuiltAt = "";
#else
            var lines = (Resources.Load<TextAsset>(ResourceName)?.text ?? "unknown").Split('\n');
            Commit = lines[0].Trim();
            BuiltAt = lines.Length > 1 ? lines[1].Trim() : "";
#endif
        }

        public static string Label => IsEditor ? $"Build {Commit} (editor)" : $"Build {Commit}, {BuiltAt}";

        // What a client sends when it connects.
        public static string Payload => $"{Commit}|{(IsEditor ? "editor" : "build")}";

        // Same commit plays together. Editor copies always do, so Multiplayer Play Mode works.
        public static bool Matches(string payload, out string theirCommit)
        {
            var parts = (payload ?? "").Split('|');
            theirCommit = parts[0].Length > 0 ? parts[0] : "unknown";
            bool theyAreEditor = parts.Length > 1 && parts[1] == "editor";
            return (IsEditor && theyAreEditor) || theirCommit == Commit;
        }

#if UNITY_EDITOR
        public static string ReadGit()
        {
            try
            {
                string hash = Git("rev-parse --short HEAD");
                if (string.IsNullOrEmpty(hash)) return "unknown";
                bool dirty = !string.IsNullOrEmpty(Git("status --porcelain -- Assets/MallRatz"));
                return dirty ? hash + "+" : hash;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        static string Git(string arguments)
        {
            var info = new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = System.IO.Path.GetDirectoryName(Application.dataPath),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = System.Diagnostics.Process.Start(info);
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);
            return process.ExitCode == 0 ? output : null;
        }
#endif
    }
}
