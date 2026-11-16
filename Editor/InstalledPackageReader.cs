using System;
using UnityEditor.PackageManager;

namespace Tool.BeltPath.Editor
{
    /// <summary>The BeltPath package as the current project has it installed.</summary>
    public readonly struct InstalledPackage
    {
        public Version Version { get; }
        public PackageSource Source { get; }
        public string GitUrl { get; }

        public InstalledPackage(Version version, PackageSource source, string gitUrl)
        {
            Version = version;
            Source = source;
            GitUrl = gitUrl;
        }
    }

    /// <summary>Reads the installed version, source and git url of the BeltPath package.</summary>
    public static class InstalledPackageReader
    {
        public const string PackageName = "com.tool.beltpath";
        public const string DefaultGitUrl = "https://github.com/vudongphuc99/com.tool.beltpath.git";

        public static bool TryRead(out InstalledPackage installed)
        {
            installed = default;
            var info = PackageInfo.FindForPackageName(PackageName);
            if (info == null || !Version.TryParse(info.version, out var version))
            {
                return false;
            }

            var gitUrl = info.source == PackageSource.Git ? ExtractGitUrl(info.packageId) : DefaultGitUrl;
            installed = new InstalledPackage(version, info.source, gitUrl);
            return true;
        }

        /// <summary>Turns <c>name@url#revision</c> into <c>url</c>. Returns the input without the name when there is no revision.</summary>
        public static string ExtractGitUrl(string packageId)
        {
            var url = packageId;
            var nameSeparator = packageId.IndexOf("@", StringComparison.Ordinal);
            if (packageId.StartsWith(PackageName + "@", StringComparison.Ordinal) && nameSeparator >= 0)
            {
                url = packageId.Substring(nameSeparator + 1);
            }

            var revisionSeparator = url.IndexOf('#');
            return revisionSeparator >= 0 ? url.Substring(0, revisionSeparator) : url;
        }
    }
}
