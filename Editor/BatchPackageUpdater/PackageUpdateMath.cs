using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Helpers for the Batch Package Updater: parse UPM package ids and Git URLs, read `git ls-remote` output, and compare versions.
    ///     No Unity types, so EditMode tests can call everything directly.
    /// </summary>
    public static class PackageUpdateMath
    {
        private const string GitPlusPrefix = "git+";
        private const string PeeledSuffix = "^{}";


        /// <summary>
        ///     Returns the part after the first '@' of a UPM package id ("com.foo@https://host/repo.git#main" gives "https://host/repo.git#main").
        ///     Returns null when there is no source part.
        /// </summary>
        public static string GetSourceFromPackageId(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return null;
            }

            var at = packageId.IndexOf('@');

            if (at < 0 || at == packageId.Length - 1)
            {
                return null;
            }

            return packageId.Substring(at + 1);
        }


        /// <summary>
        ///     Returns the URL that git can clone: removes the "git+" prefix, the "?path=" query and the "#revision" fragment.
        /// </summary>
        public static string GetRemoteUrl(string gitUrl)
        {
            if (string.IsNullOrEmpty(gitUrl))
            {
                return null;
            }

            var url = gitUrl.StartsWith(GitPlusPrefix, StringComparison.Ordinal) ? gitUrl.Substring(GitPlusPrefix.Length) : gitUrl;

            var hash = url.IndexOf('#');

            if (hash >= 0)
            {
                url = url.Substring(0, hash);
            }

            var query = url.IndexOf('?');

            if (query >= 0)
            {
                url = url.Substring(0, query);
            }

            return url;
        }


        /// <summary>
        ///     Returns the revision after '#' (branch, tag or commit hash), or null when the URL has none.
        /// </summary>
        public static string GetRevision(string gitUrl)
        {
            if (string.IsNullOrEmpty(gitUrl))
            {
                return null;
            }

            var hash = gitUrl.IndexOf('#');

            if (hash < 0 || hash == gitUrl.Length - 1)
            {
                return null;
            }

            return gitUrl.Substring(hash + 1);
        }


        /// <summary>
        ///     True when the revision looks like a commit hash (7 to 40 hex characters). Such a package is pinned and never updates.
        /// </summary>
        public static bool IsCommitHash(string revision)
        {
            if (string.IsNullOrEmpty(revision) || revision.Length < 7 || revision.Length > 40)
            {
                return false;
            }

            foreach (var c in revision)
            {
                var isHex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        ///     Returns the commit hash from `git ls-remote` output. Prefers a peeled annotated tag line ("^{}"), because that line holds the commit.
        ///     Returns null when the output has no valid line.
        /// </summary>
        public static string ParseLsRemoteHash(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            string first = null;

            foreach (var rawLine in output.Split('\n'))
            {
                var line = rawLine.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                var parts = line.Split(new[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2 || !IsCommitHash(parts[0]))
                {
                    continue;
                }

                if (parts[1].EndsWith(PeeledSuffix, StringComparison.Ordinal))
                {
                    return parts[0];
                }

                first ??= parts[0];
            }

            return first;
        }


        /// <summary>
        ///     Returns the `git ls-remote` patterns for a revision. `git ls-remote` patterns match the end of a ref name, so a bare
        ///     "main" would also match "refs/heads/feature/main". A named revision is therefore asked for by its exact refs:
        ///     "refs/heads/rev", "refs/tags/rev" and "refs/tags/rev^{}". "HEAD" (or no revision) and a full ref name ("refs/...") are used as is.
        /// </summary>
        public static string[] GetLsRemotePatterns(string revision)
        {
            if (string.IsNullOrEmpty(revision) || revision == "HEAD")
            {
                return new[] {"HEAD"};
            }

            if (revision.StartsWith("refs/", StringComparison.Ordinal))
            {
                return new[] {revision};
            }

            return new[] {$"refs/heads/{revision}", $"refs/tags/{revision}", $"refs/tags/{revision}{PeeledSuffix}"};
        }


        /// <summary>
        ///     Returns the commit hash for <paramref name="revision" /> from `git ls-remote` output.
        ///     Prefers an exact ref-name match: the peeled tag ("refs/tags/rev^{}"), then the branch ("refs/heads/rev"), then the tag ("refs/tags/rev").
        ///     A full ref name ("refs/...") matches itself or its peeled line. For "HEAD" or no revision, and when no exact match exists,
        ///     falls back to <see cref="ParseLsRemoteHash(string)" />.
        /// </summary>
        public static string ParseLsRemoteHash(string output, string revision)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            if (string.IsNullOrEmpty(revision) || revision == "HEAD")
            {
                return ParseLsRemoteHash(output);
            }

            string[] preferredRefs;

            if (revision.StartsWith("refs/", StringComparison.Ordinal))
            {
                preferredRefs = new[] {revision + PeeledSuffix, revision};
            }
            else
            {
                preferredRefs = new[] {$"refs/tags/{revision}{PeeledSuffix}", $"refs/heads/{revision}", $"refs/tags/{revision}"};
            }

            var hashes = new string[preferredRefs.Length];

            foreach (var rawLine in output.Split('\n'))
            {
                var parts = rawLine.Trim().Split(new[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2 || !IsCommitHash(parts[0]))
                {
                    continue;
                }

                var index = Array.IndexOf(preferredRefs, parts[1]);

                if (index >= 0 && hashes[index] == null)
                {
                    hashes[index] = parts[0];
                }
            }

            foreach (var hash in hashes)
            {
                if (hash != null)
                {
                    return hash;
                }
            }

            return ParseLsRemoteHash(output);
        }


        /// <summary>
        ///     True when both hashes name the same commit. Accepts a short hash on either side.
        /// </summary>
        public static bool IsSameCommit(string installedHash, string remoteHash)
        {
            if (string.IsNullOrEmpty(installedHash) || string.IsNullOrEmpty(remoteHash))
            {
                return false;
            }

            return installedHash.StartsWith(remoteHash, StringComparison.OrdinalIgnoreCase) || remoteHash.StartsWith(installedHash, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        ///     True when latest is a higher version than installed.
        /// </summary>
        public static bool HasRegistryUpdate(string installedVersion, string latestVersion)
        {
            if (string.IsNullOrEmpty(installedVersion) || string.IsNullOrEmpty(latestVersion))
            {
                return false;
            }

            return CompareVersions(latestVersion, installedVersion) > 0;
        }


        /// <summary>
        ///     Compares two SemVer strings. Returns a negative number when a is lower, 0 when equal, positive when a is higher.
        ///     A release is higher than a pre-release with the same core ("1.0.0" > "1.0.0-pre.1"). Build metadata ("+...") is ignored.
        /// </summary>
        public static int CompareVersions(string a, string b)
        {
            SplitVersion(a, out var coreA, out var preA);
            SplitVersion(b, out var coreB, out var preB);

            var partsA = coreA.Split('.');
            var partsB = coreB.Split('.');
            var length = Math.Max(partsA.Length, partsB.Length);

            for (var i = 0; i < length; i++)
            {
                var numberA = i < partsA.Length && int.TryParse(partsA[i], out var parsedA) ? parsedA : 0;
                var numberB = i < partsB.Length && int.TryParse(partsB[i], out var parsedB) ? parsedB : 0;

                if (numberA != numberB)
                {
                    return numberA.CompareTo(numberB);
                }
            }

            if (preA == null && preB == null)
            {
                return 0;
            }

            if (preA == null)
            {
                return 1;
            }

            if (preB == null)
            {
                return -1;
            }

            return ComparePreRelease(preA, preB);
        }


        /// <summary>
        ///     Returns the repository-relative path of the package.json from the "?path=" part of a Git URL.
        ///     "https://host/repo.git?path=/Packages/com.foo#main" gives "Packages/com.foo/package.json". No path gives "package.json".
        /// </summary>
        public static string GetPackageJsonPath(string gitUrl)
        {
            const string pathKey = "path=";

            var subPath = string.Empty;

            if (!string.IsNullOrEmpty(gitUrl))
            {
                var url = gitUrl;
                var hash = url.IndexOf('#');

                if (hash >= 0)
                {
                    url = url.Substring(0, hash);
                }

                var query = url.IndexOf('?');

                if (query >= 0)
                {
                    foreach (var parameter in url.Substring(query + 1).Split('&'))
                    {
                        if (parameter.StartsWith(pathKey, StringComparison.Ordinal))
                        {
                            subPath = Uri.UnescapeDataString(parameter.Substring(pathKey.Length)).Trim('/');
                        }
                    }
                }
            }

            return subPath.Length == 0 ? "package.json" : $"{subPath}/package.json";
        }


        /// <summary>
        ///     Returns the value of the first "version" key in package.json text, or null when there is none.
        /// </summary>
        public static string ParsePackageJsonVersion(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var match = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]*)\"");

            return match.Success && match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : null;
        }


        /// <summary>
        ///     Message for a Git package with a newer commit: "0.7.0 → 0.8.0  (abc1234 → def5678)".
        ///     Without a remote version only the commits are shown.
        /// </summary>
        public static string FormatGitUpdateMessage(string installedVersion, string remoteVersion, string installedHash, string remoteHash)
        {
            var commits = $"{ShortHash(installedHash)} → {ShortHash(remoteHash)}";

            if (string.IsNullOrEmpty(remoteVersion))
            {
                return commits;
            }

            return $"{installedVersion} → {remoteVersion}  ({commits})";
        }


        /// <summary>
        ///     Writes pending updates (package name, expected commit hash or version) as "name\texpected" lines.
        ///     The expected value may be empty when it is unknown.
        /// </summary>
        public static string SerializePendingUpdates(IEnumerable<KeyValuePair<string, string>> updates)
        {
            if (updates == null)
            {
                return string.Empty;
            }

            return string.Join("\n", updates.Where(u => !string.IsNullOrEmpty(u.Key)).Select(u => $"{u.Key}\t{u.Value ?? string.Empty}"));
        }


        /// <summary>
        ///     Reads the text from <see cref="SerializePendingUpdates" /> back into package name → expected commit hash or version.
        /// </summary>
        public static Dictionary<string, string> ParsePendingUpdates(string text)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');

                if (line.Length == 0)
                {
                    continue;
                }

                var tab = line.IndexOf('\t');
                var name = tab < 0 ? line : line.Substring(0, tab);
                var expected = tab < 0 ? string.Empty : line.Substring(tab + 1);

                if (name.Length > 0)
                {
                    result[name] = expected;
                }
            }

            return result;
        }


        /// <summary>
        ///     True when the installed package matches what the update expected: the commit for Git packages, the version for registry packages.
        ///     An empty expected value can't be checked and counts as applied.
        /// </summary>
        public static bool IsUpdateApplied(bool isGit, string installedVersion, string installedHash, string expected)
        {
            if (string.IsNullOrEmpty(expected))
            {
                return true;
            }

            return isGit ? IsSameCommit(installedHash, expected) : string.Equals(installedVersion, expected, StringComparison.Ordinal);
        }


        /// <summary>
        ///     Returns the first 7 characters of a hash, or the hash itself when shorter.
        /// </summary>
        public static string ShortHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return string.Empty;
            }

            return hash.Length <= 7 ? hash : hash.Substring(0, 7);
        }


        private static void SplitVersion(string version, out string core, out string preRelease)
        {
            version ??= string.Empty;

            var plus = version.IndexOf('+');

            if (plus >= 0)
            {
                version = version.Substring(0, plus);
            }

            var dash = version.IndexOf('-');

            if (dash >= 0)
            {
                core = version.Substring(0, dash);
                preRelease = version.Substring(dash + 1);
            }
            else
            {
                core = version;
                preRelease = null;
            }
        }


        private static int ComparePreRelease(string a, string b)
        {
            var partsA = a.Split('.');
            var partsB = b.Split('.');
            var length = Math.Min(partsA.Length, partsB.Length);

            for (var i = 0; i < length; i++)
            {
                var isNumberA = int.TryParse(partsA[i], out var numberA);
                var isNumberB = int.TryParse(partsB[i], out var numberB);

                int result;

                if (isNumberA && isNumberB)
                {
                    result = numberA.CompareTo(numberB);
                }
                else if (isNumberA)
                {
                    result = -1; // SemVer: numeric identifiers are lower than alphanumeric ones
                }
                else if (isNumberB)
                {
                    result = 1;
                }
                else
                {
                    result = string.CompareOrdinal(partsA[i], partsB[i]);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return partsA.Length.CompareTo(partsB.Length);
        }
    }
}
