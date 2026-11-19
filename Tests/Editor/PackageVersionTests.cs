using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Tool.BeltPath.Tests
{
    /// <summary>Guards the release flow: the version in package.json must match the newest CHANGELOG entry.</summary>
    public sealed class PackageVersionTests
    {
        private const string PackageName = "com.tool.beltpath";
        private static readonly Regex JsonVersion = new Regex("\"version\"\\s*:\\s*\"([^\"]+)\"");
        private static readonly Regex ChangelogVersion = new Regex(@"^## \[([^\]]+)\]", RegexOptions.Multiline);

        [Test]
        public void PackageJsonVersionMatchesNewestChangelogEntry()
        {
            var root = PackageInfo.FindForPackageName(PackageName).resolvedPath;

            var jsonMatch = JsonVersion.Match(File.ReadAllText(Path.Combine(root, "package.json")));
            var changelogMatch = ChangelogVersion.Match(File.ReadAllText(Path.Combine(root, "CHANGELOG.md")));

            Assert.IsTrue(jsonMatch.Success, "package.json has no version.");
            Assert.IsTrue(changelogMatch.Success, "CHANGELOG.md has no version entry.");
            Assert.AreEqual(jsonMatch.Groups[1].Value, changelogMatch.Groups[1].Value);
        }
    }
}
