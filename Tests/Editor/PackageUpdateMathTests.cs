using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;


namespace SOSXR.EditorSpice.Tests
{
    public class PackageUpdateMathTests
    {
        private const string HashA = "0123456789abcdef0123456789abcdef01234567";
        private const string HashB = "fedcba9876543210fedcba9876543210fedcba98";


        [TestCase("com.foo@https://github.com/a/b.git#main", "https://github.com/a/b.git#main")]
        [TestCase("com.foo@git@github.com:a/b.git", "git@github.com:a/b.git")]
        [TestCase("com.foo@1.2.3", "1.2.3")]
        [TestCase("com.foo", null)]
        [TestCase("com.foo@", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void GetSourceFromPackageId_ReturnsPartAfterFirstAt(string packageId, string expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.GetSourceFromPackageId(packageId));
        }


        [TestCase("https://github.com/a/b.git", "https://github.com/a/b.git")]
        [TestCase("https://github.com/a/b.git#main", "https://github.com/a/b.git")]
        [TestCase("https://github.com/a/b.git?path=Packages/com.foo#v1.0", "https://github.com/a/b.git")]
        [TestCase("git+https://github.com/a/b#dev", "https://github.com/a/b")]
        [TestCase("git+ssh://git@github.com/a/b.git", "ssh://git@github.com/a/b.git")]
        [TestCase(null, null)]
        public void GetRemoteUrl_StripsPrefixQueryAndRevision(string url, string expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.GetRemoteUrl(url));
        }


        [TestCase("https://github.com/a/b.git#main", "main")]
        [TestCase("https://github.com/a/b.git?path=sub#v1.0", "v1.0")]
        [TestCase("https://github.com/a/b.git", null)]
        [TestCase("https://github.com/a/b.git#", null)]
        [TestCase(null, null)]
        public void GetRevision_ReturnsFragment(string url, string expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.GetRevision(url));
        }


        [TestCase(HashA, true)]
        [TestCase("abc1234", true)]
        [TestCase("ABC1234", true)]
        [TestCase("abc123", false)] // too short
        [TestCase("main", false)]
        [TestCase("v1.0.0", false)]
        [TestCase("abc123g", false)]
        [TestCase(null, false)]
        public void IsCommitHash_DetectsHexHashes(string revision, bool expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.IsCommitHash(revision));
        }


        [Test]
        public void ParseLsRemoteHash_SingleBranchLine_ReturnsHash()
        {
            Assert.AreEqual(HashA, PackageUpdateMath.ParseLsRemoteHash($"{HashA}\trefs/heads/main\n"));
        }


        [Test]
        public void ParseLsRemoteHash_AnnotatedTag_ReturnsPeeledCommit()
        {
            var output = $"{HashA}\trefs/tags/v1.0\n{HashB}\trefs/tags/v1.0^{{}}\n";

            Assert.AreEqual(HashB, PackageUpdateMath.ParseLsRemoteHash(output));
        }


        [Test]
        public void ParseLsRemoteHash_WindowsLineEndings_ReturnsHash()
        {
            Assert.AreEqual(HashA, PackageUpdateMath.ParseLsRemoteHash($"{HashA}\tHEAD\r\n"));
        }


        [TestCase("")]
        [TestCase("   \n")]
        [TestCase(null)]
        [TestCase("fatal: repository not found")]
        public void ParseLsRemoteHash_NoValidLine_ReturnsNull(string output)
        {
            Assert.IsNull(PackageUpdateMath.ParseLsRemoteHash(output));
        }


        [Test]
        public void GetLsRemotePatterns_NamedRevision_AsksForExactRefs()
        {
            CollectionAssert.AreEqual(new[] {"refs/heads/main", "refs/tags/main", "refs/tags/main^{}"}, PackageUpdateMath.GetLsRemotePatterns("main"));
        }


        [TestCase(null)]
        [TestCase("")]
        [TestCase("HEAD")]
        public void GetLsRemotePatterns_NoRevisionOrHead_AsksForHead(string revision)
        {
            CollectionAssert.AreEqual(new[] {"HEAD"}, PackageUpdateMath.GetLsRemotePatterns(revision));
        }


        [Test]
        public void GetLsRemotePatterns_FullRefName_IsUsedAsIs()
        {
            CollectionAssert.AreEqual(new[] {"refs/heads/main"}, PackageUpdateMath.GetLsRemotePatterns("refs/heads/main"));
        }


        [Test]
        public void ParseLsRemoteHash_WithRevision_IgnoresTailMatchOnOtherBranch()
        {
            var output = $"{HashA}\trefs/heads/feature/main\n{HashB}\trefs/heads/main\n";

            Assert.AreEqual(HashB, PackageUpdateMath.ParseLsRemoteHash(output, "main"));
        }


        [Test]
        public void ParseLsRemoteHash_WithRevision_PrefersPeeledTagThenBranchThenTag()
        {
            const string hashC = "1111111111111111111111111111111111111111";
            var output = $"{HashA}\trefs/tags/v1\n{HashB}\trefs/heads/v1\n{hashC}\trefs/tags/v1^{{}}\n";

            Assert.AreEqual(hashC, PackageUpdateMath.ParseLsRemoteHash(output, "v1"));
            Assert.AreEqual(HashB, PackageUpdateMath.ParseLsRemoteHash($"{HashA}\trefs/tags/v1\n{HashB}\trefs/heads/v1\n", "v1"));
            Assert.AreEqual(HashA, PackageUpdateMath.ParseLsRemoteHash($"{HashA}\trefs/tags/v1\n", "v1"));
        }


        [Test]
        public void ParseLsRemoteHash_WithHeadRevision_ReturnsHeadHash()
        {
            Assert.AreEqual(HashA, PackageUpdateMath.ParseLsRemoteHash($"{HashA}\tHEAD\n", "HEAD"));
        }


        [Test]
        public void Entry_RegistryUpToDate_CannotUpdate()
        {
            var entry = new BatchPackageUpdater.Entry
            {
                Source = UnityEditor.PackageManager.PackageSource.Registry,
                AddIdentifier = "com.foo@1.0.0",
                State = BatchPackageUpdater.UpdateState.UpToDate
            };

            Assert.IsFalse(entry.CanUpdate);

            entry.State = BatchPackageUpdater.UpdateState.UpdateAvailable;

            Assert.IsTrue(entry.CanUpdate);
        }


        [Test]
        public void Entry_GitUpToDate_CanStillUpdate()
        {
            var entry = new BatchPackageUpdater.Entry
            {
                Source = UnityEditor.PackageManager.PackageSource.Git,
                AddIdentifier = "https://github.com/a/b.git#main",
                State = BatchPackageUpdater.UpdateState.UpToDate
            };

            Assert.IsTrue(entry.CanUpdate);
        }


        [TestCase(HashA, HashA, true)]
        [TestCase(HashA, "0123456", true)]
        [TestCase("0123456", HashA, true)]
        [TestCase(HashA, HashB, false)]
        [TestCase(null, HashA, false)]
        [TestCase(HashA, null, false)]
        public void IsSameCommit_ComparesFullAndShortHashes(string installed, string remote, bool expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.IsSameCommit(installed, remote));
        }


        [TestCase("1.0.0", "1.0.1", true)]
        [TestCase("1.0.1", "1.0.0", false)]
        [TestCase("1.0.0", "1.0.0", false)]
        [TestCase("1.0.0-pre.1", "1.0.0", true)]
        [TestCase("1.0.0", "1.0.0-pre.1", false)]
        [TestCase("1.9.0", "1.10.0", true)]
        [TestCase("1.0.0", null, false)]
        [TestCase(null, "1.0.0", false)]
        public void HasRegistryUpdate_ComparesSemVer(string installed, string latest, bool expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.HasRegistryUpdate(installed, latest));
        }


        [TestCase("1.0.0-pre.2", "1.0.0-pre.10", -1)]
        [TestCase("1.0.0-alpha", "1.0.0-beta", -1)]
        [TestCase("1.0.0-1", "1.0.0-alpha", -1)]
        [TestCase("1.0.0-alpha", "1.0.0-alpha.1", -1)]
        [TestCase("1.0.0+build.5", "1.0.0", 0)]
        [TestCase("2.0", "2.0.0", 0)]
        public void CompareVersions_FollowsSemVerOrdering(string a, string b, int expectedSign)
        {
            Assert.AreEqual(expectedSign, System.Math.Sign(PackageUpdateMath.CompareVersions(a, b)));
        }


        [TestCase(HashA, "0123456")]
        [TestCase("abc", "abc")]
        [TestCase(null, "")]
        public void ShortHash_TruncatesToSeven(string hash, string expected)
        {
            Assert.AreEqual(expected, PackageUpdateMath.ShortHash(hash));
        }
    }
}
