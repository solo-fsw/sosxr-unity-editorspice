#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SOSXR.BuildHelpers
{
    /// <summary>
    ///     Handles automatic semantic versioning and build information updates.
    ///     Each build gets the version of the last successful build plus one.
    ///     Failed, cancelled or unfinished builds therefore do not consume version numbers.
    /// </summary>
    public class BuildInfoManager : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static BuildInfoDetails _buildInfoDetails;

        /// <summary>
        /// Called after a build completes. Triggers post-build processing to record results and update build metadata.
        /// </summary>
        /// <param name="report"></param>
        public void OnPostprocessBuild(BuildReport report)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(PostProcessBuildCR());
        }

        public int callbackOrder => 0;

        /// <summary>
        /// Called before a build starts. Captures pre-build state and increments attempted builds.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            GetBuildInfoDetailsFile();

            _buildInfoDetails.TotalAttemptedBuilds++;

            SeedLastSuccessfulVersion(
                _buildInfoDetails,
                PlayerSettings.bundleVersion,
                PlayerSettings.Android.bundleVersionCode
            );

            ApplyNextVersion();

            _buildInfoDetails.LastBuildResult = BuildResult.Unknown; // Reset the result before the build starts

            SaveBuildInfoDetails();

            WritePreBuildInfoToFile();
        }


        /// <summary>
        ///     Marks the ScriptableObject dirty and writes it to disk.
        ///     Without this the counters and LastBuildResult are lost when the editor closes.
        /// </summary>
        private static void SaveBuildInfoDetails()
        {
            if (_buildInfoDetails == null)
            {
                return;
            }

            EditorUtility.SetDirty(_buildInfoDetails);
            AssetDatabase.SaveAssetIfDirty(_buildInfoDetails);
        }

        private static void GetBuildInfoDetailsFile()
        {
            if (_buildInfoDetails != null)
            {
                return;
            }

            var path = GetAssetPath();

            if (path == null)
            {
                _buildInfoDetails = ScriptableObject.CreateInstance<BuildInfoDetails>();

                // No previous build exists, so there is no failed increment to undo
                _buildInfoDetails.LastBuildResult = BuildResult.Succeeded;

                var assetPath = "Assets/_SOSXR/Resources/BuildInfoDetails.asset";
                EnsureDirectoryExists(Path.GetDirectoryName(assetPath));
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(_buildInfoDetails, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"A config file has been created at {assetPath}. You can move this anywhere you'd like."
                );
            }
            else
            {
                _buildInfoDetails = AssetDatabase.LoadAssetAtPath<BuildInfoDetails>(path);
            }
        }

        /// <summary>
        ///     Projects that used the old undo/increment logic have no last successful version stored yet.
        ///     Derive it once from the current PlayerSettings values.
        ///     If the previous build did not succeed, the current values belong to that build, so the last success is one lower.
        /// </summary>
        public static void SeedLastSuccessfulVersion(
            BuildInfoDetails details,
            string currentSemVer,
            int currentBundleVersionCode
        )
        {
            if (!string.IsNullOrEmpty(details.LastSuccessfulSemVer))
            {
                return;
            }

            var previousBuildSucceeded = details.LastBuildResult == BuildResult.Succeeded;

            details.LastSuccessfulSemVer = previousBuildSucceeded
                ? currentSemVer
                : GenerateNextVersion(
                    currentSemVer,
                    false,
                    IsDevelopmentVersion(currentSemVer, details),
                    details.DevelopmentBuildIndicator,
                    details.ProductionBuildIndicator,
                    details.InitialSemVer
                );

            details.LastSuccessfulBundleVersionCode = previousBuildSucceeded
                ? currentBundleVersionCode
                : Math.Max(0, currentBundleVersionCode - 1);
        }


        /// <summary>
        ///     The version for the upcoming build: one higher than the last successful build.
        /// </summary>
        public static (string semVer, int bundleVersionCode) GetNextVersion(
            BuildInfoDetails details,
            bool development
        )
        {
            var semVer = GenerateNextVersion(
                details.LastSuccessfulSemVer,
                true,
                development,
                details.DevelopmentBuildIndicator,
                details.ProductionBuildIndicator,
                details.InitialSemVer
            );

            var bundleVersionCode = Math.Max(0, details.LastSuccessfulBundleVersionCode) + 1;

            return (semVer, bundleVersionCode);
        }


        /// <summary>
        ///     Stores the version of the build that just succeeded, so the next build continues from it.
        /// </summary>
        public static void RecordSuccessfulBuild(BuildInfoDetails details)
        {
            details.LastSuccessfulSemVer = details.SemVer;
            details.LastSuccessfulBundleVersionCode = details.AndroidBundleVersionCode;
        }


        private static bool IsDevelopmentVersion(string semVer, BuildInfoDetails details)
        {
            return !string.IsNullOrEmpty(semVer)
                && !string.IsNullOrEmpty(details.DevelopmentBuildIndicator)
                && semVer.EndsWith(details.DevelopmentBuildIndicator);
        }


        /// <summary>
        ///     Writes the next version to the BuildInfoDetails and PlayerSettings.
        /// </summary>
        private static void ApplyNextVersion()
        {
            var (semVer, bundleVersionCode) = GetNextVersion(
                _buildInfoDetails,
                EditorUserBuildSettings.development
            );

            _buildInfoDetails.OldSemVer = PlayerSettings.bundleVersion;
            _buildInfoDetails.SemVer = semVer;
            PlayerSettings.bundleVersion = semVer;

            Debug.LogFormat(
                "SemanticVersion: Updated SemVer from {0} to {1}",
                _buildInfoDetails.OldSemVer,
                _buildInfoDetails.SemVer
            );

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                return;
            }

            _buildInfoDetails.OldBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _buildInfoDetails.AndroidBundleVersionCode = bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;

            Debug.LogFormat(
                "SemanticVersion: Updated Android Bundle Version Code from {0} to {1}",
                _buildInfoDetails.OldBundleVersionCode,
                _buildInfoDetails.AndroidBundleVersionCode
            );
        }

        private static IEnumerator PostProcessBuildCR()
        {
            var seconds = 1;
            Debug.Log($"Waiting {seconds} seconds to ensure BuildReport is ready...");

            yield return new EditorWaitForSeconds(seconds);

            Debug.Log("Waited enough, fetching latest BuildReport...");

            WritePostBuildInfoToFile();
        }

        [MenuItem("SOSXR/Build Info/Print Build Size")]
        private static void TestGettingSize()
        {
            var buildReport = BuildReport.GetLatestReport();

            var fileInfo = new FileInfo(buildReport.summary.outputPath);
            var fileSizeMB = Math.Round(fileInfo.Length / (1000f * 1000f), 1);

            Debug.Log($"Build size: {fileSizeMB} MB");
        }

        /// <summary>
        /// Writes the post-build information to the CSV and updates the last build result.
        /// </summary>
        public static void WritePostBuildInfoToFile()
        {
            // A domain reload during the build clears the static reference
            GetBuildInfoDetailsFile();

            var buildReport = BuildReport.GetLatestReport();

            if (buildReport == null)
            {
                return;
            }

            double fileSizeMB = -1;
            var fileInfo = new FileInfo(buildReport.summary.outputPath);

            if (fileInfo.Exists)
            {
                fileSizeMB = Math.Round(fileInfo.Length / (1000f * 1000f), 1);
            }

            if (buildReport.summary.result == BuildResult.Succeeded)
            {
                _buildInfoDetails.TotalSuccessBuilds++;
                RecordSuccessfulBuild(_buildInfoDetails);
            }

            var buildInfo = new[]
            {
                string.Concat(
                    ", ",
                    Math.Round(buildReport.summary.totalTime.TotalSeconds, 0).ToString()
                ),
                fileSizeMB.ToString(),
                _buildInfoDetails.TotalSuccessBuilds.ToString(),
                buildReport.summary.result.ToString(),
            };

            using var sw = File.AppendText(_buildInfoDetails.FilePath);

            sw.Write(string.Join(", ", buildInfo));

            _buildInfoDetails.LastBuildResult = buildReport.summary.result;

            SaveBuildInfoDetails();
        }

        private static string GetAssetPath()
        {
            var paths = AssetDatabase
                .FindAssets($"t:{nameof(BuildInfoDetails)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(c => c.EndsWith(".asset"))
                .ToList();

            if (paths.Count > 1)
            {
                Debug.LogWarning(
                    "Multiple Build Info Detail assets found. Delete until you have only one."
                );
            }

            return paths.FirstOrDefault();
        }

        /// <summary>
        ///     Pure version of the patch increment. Separated so it can be unit tested.
        /// </summary>
        public static string GenerateNextVersion(
            string version,
            bool increment,
            bool development,
            string developmentIndicator,
            string productionIndicator,
            string initialSemVer
        )
        {
            var buildIndicator = development ? developmentIndicator : productionIndicator;
            version = (version ?? string.Empty).TrimEnd(
                Convert.ToChar(developmentIndicator),
                Convert.ToChar(productionIndicator)
            ); // Remove build indicator if present

            var parts = version.Split('_');

            if (parts.Length < 2 || !int.TryParse(parts[^1], out var currentNumber))
            {
                var initialVersion = $"{initialSemVer}{buildIndicator}";

                Debug.LogWarning(
                    $"[SemanticVersion] Invalid version format \"{version}\". "
                        + $"Expected format like '0_0_0{buildIndicator}' or 'alpha-1_32_2{buildIndicator}'. Resetting to initial version {initialVersion}"
                );

                return initialVersion;
            }

            parts[^1] = (increment ? currentNumber + 1 : Math.Max(0, currentNumber - 1)).ToString();

            return $"{string.Join("_", parts)}{buildIndicator}";
        }

        public static void WritePreBuildInfoToFile()
        {
            var directoryPath = Path.GetDirectoryName(_buildInfoDetails.FilePath) ?? string.Empty;

            EnsureDirectoryExists(directoryPath);

            if (!File.Exists(_buildInfoDetails.FilePath))
            {
                WriteHeadersToFile();
            }

            using (var sw = File.AppendText(_buildInfoDetails.FilePath))
            {
                sw.WriteLine();
            }

            AppendBuildInfoToFile();

            Debug.Log(
                $"SemanticVersion: Appended build information to {_buildInfoDetails.FilePath}"
            );
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Writes CSV headers to the build info file if needed.
        /// </summary>
        public static void WriteHeadersToFile()
        {
            using var sw = File.CreateText(_buildInfoDetails.FilePath);
            sw.WriteLine(
                "PackageName, UnityVersion, TotalAttemptedBuilds, BuildTarget, ScriptingBackend, APICompatibility, ProductionBuild, SemVer, AndroidBundleCode, BuildDate, BuildTime, BuildDurationSec, BuildSizeMB, TotalSuccessBuilds, Result"
            );
        }

        private static void AppendBuildInfoToFile()
        {
            var buildInfo = new[]
            {
                PlayerSettings.applicationIdentifier,
                Application.unityVersion,
                _buildInfoDetails.TotalAttemptedBuilds.ToString(),
                EditorUserBuildSettings.activeBuildTarget.ToString(),
                PlayerSettings
                    .GetScriptingBackend(
                        NamedBuildTarget.FromBuildTargetGroup(
                            EditorUserBuildSettings.selectedBuildTargetGroup
                        )
                    )
                    .ToString(),
                PlayerSettings
                    .GetApiCompatibilityLevel(
                        NamedBuildTarget.FromBuildTargetGroup(
                            EditorUserBuildSettings.selectedBuildTargetGroup
                        )
                    )
                    .ToString(),
                (!EditorUserBuildSettings.development).ToString(),
                PlayerSettings.bundleVersion,
                PlayerSettings.Android.bundleVersionCode.ToString(),
                DateTime.Now.ToString("yyyy-MM-dd"),
                DateTime.Now.ToString("HH:mm"),
            };

            using var sw = File.AppendText(_buildInfoDetails.FilePath);
            sw.Write(string.Join(", ", buildInfo));
        }
    }
}
#endif
