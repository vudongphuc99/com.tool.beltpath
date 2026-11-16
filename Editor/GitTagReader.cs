using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Tool.BeltPath.Editor
{
    /// <summary>Outcome of a remote tag lookup: the newest <c>vX.Y.Z</c> tag, or an error message.</summary>
    public readonly struct GitTagResult
    {
        public bool Success { get; }
        public string Tag { get; }
        public Version Version { get; }
        public string Error { get; }

        private GitTagResult(bool success, string tag, Version version, string error)
        {
            Success = success;
            Tag = tag;
            Version = version;
            Error = error;
        }

        public static GitTagResult Found(string tag, Version version) => new GitTagResult(true, tag, version, null);

        public static GitTagResult Failed(string error) => new GitTagResult(false, null, null, error);
    }

    /// <summary>Finds the newest release tag of a git repository with <c>git ls-remote</c>.</summary>
    public static class GitTagReader
    {
        private const string TagRefPrefix = "refs/tags/v";
        private const int TimeoutMilliseconds = 15000;

        public static Task<GitTagResult> ReadLatestAsync(string repositoryUrl)
        {
            return Task.Run(() => ReadLatest(repositoryUrl));
        }

        /// <summary>Parses <c>git ls-remote --tags --refs</c> output and returns the highest <c>vX.Y.Z</c> tag.</summary>
        public static bool TryParseLatest(string lsRemoteOutput, out string tag, out Version version)
        {
            tag = null;
            version = null;
            if (string.IsNullOrEmpty(lsRemoteOutput))
            {
                return false;
            }

            foreach (var line in lsRemoteOutput.Split('\n'))
            {
                if (!TryParseLine(line, out var lineTag, out var lineVersion))
                {
                    continue;
                }

                if (version == null || lineVersion > version)
                {
                    tag = lineTag;
                    version = lineVersion;
                }
            }

            return version != null;
        }

        private static bool TryParseLine(string line, out string tag, out Version version)
        {
            tag = null;
            version = null;
            var refIndex = line.IndexOf(TagRefPrefix, StringComparison.Ordinal);
            if (refIndex < 0)
            {
                return false;
            }

            tag = line.Substring(refIndex + "refs/tags/".Length).Trim();
            return Version.TryParse(tag.Substring(1), out version);
        }

        private static GitTagResult ReadLatest(string repositoryUrl)
        {
            try
            {
                var output = RunLsRemote(repositoryUrl);
                if (TryParseLatest(output, out var tag, out var version))
                {
                    return GitTagResult.Found(tag, version);
                }

                return GitTagResult.Failed("No vX.Y.Z tags found in the repository.");
            }
            catch (Exception e) when (e is InvalidOperationException || e is System.ComponentModel.Win32Exception)
            {
                return GitTagResult.Failed($"Could not run git: {e.Message}");
            }
        }

        private static string RunLsRemote(string repositoryUrl)
        {
            var startInfo = new ProcessStartInfo("git", $"ls-remote --tags --refs \"{repositoryUrl}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            using (var process = Process.Start(startInfo))
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    process.Kill();
                    throw new InvalidOperationException("git ls-remote timed out.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(errorTask.Result.Trim());
                }

                return outputTask.Result;
            }
        }
    }
}
