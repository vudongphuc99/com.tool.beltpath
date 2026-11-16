using NUnit.Framework;
using Tool.BeltPath.Editor;

namespace Tool.BeltPath.Tests
{
    public sealed class GitTagReaderTests
    {
        [Test]
        public void TryParseLatest_PicksHighestVersionNotLexicographic()
        {
            const string output =
                "aaa\trefs/tags/v1.9.0\nbbb\trefs/tags/v1.10.0\nccc\trefs/tags/v1.2.0\n";

            Assert.IsTrue(GitTagReader.TryParseLatest(output, out var tag, out var version));
            Assert.AreEqual("v1.10.0", tag);
            Assert.AreEqual(new System.Version(1, 10, 0), version);
        }

        [Test]
        public void TryParseLatest_IgnoresTagsThatAreNotVersions()
        {
            const string output = "aaa\trefs/tags/vNext\nbbb\trefs/tags/v1.0.0\nccc\trefs/tags/release-2\n";

            Assert.IsTrue(GitTagReader.TryParseLatest(output, out var tag, out _));
            Assert.AreEqual("v1.0.0", tag);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("aaa\trefs/tags/beta\n")]
        public void TryParseLatest_ReturnsFalseWithoutVersionTags(string output)
        {
            Assert.IsFalse(GitTagReader.TryParseLatest(output, out _, out _));
        }

        [TestCase("com.tool.beltpath@https://github.com/x/y.git#abc123", "https://github.com/x/y.git")]
        [TestCase("com.tool.beltpath@git@github.com:x/y.git#v1.0.0", "git@github.com:x/y.git")]
        [TestCase("com.tool.beltpath@https://github.com/x/y.git", "https://github.com/x/y.git")]
        public void ExtractGitUrl_DropsNameAndRevision(string packageId, string expected)
        {
            Assert.AreEqual(expected, InstalledPackageReader.ExtractGitUrl(packageId));
        }
    }
}
