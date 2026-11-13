using System.IO;
using SOSXR.EditorSpice.EditorScripts;
using UnityEditor.Build.Reporting;
using UnityEngine;


namespace SOSXR.BuildHelpers
{
    /// <summary>
    /// Stores persistent build information and configuration used by the BuildInfoManager.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildInfoDetails", menuName = "SOSXR/BuildInfoDetails")]
    public class BuildInfoDetails : ScriptableObject
    {
        [Tooltip("The file path to store the build info CSV file")]
        [SerializeField] private string m_filePath = "Assets/_SOSXR/Resources/build_info.csv";

        [Tooltip("The initial semantic version of the project, set if the semVer is not similar to this format")]
        public string InitialSemVer = "0_0_1";

        [Tooltip("The indicator to append to the semVer for development builds")]
        public string DevelopmentBuildIndicator = "d";
        [Tooltip("The indicator to append to the semVer for production builds")]
        public string ProductionBuildIndicator = "p";

        /// <summary>
        /// SemVer value prior to the most recent change.
        /// </summary>
        [HideInInspector] public string OldSemVer;
        /// <summary>
        /// Android bundle version code prior to the most recent change.
        /// </summary>
        [HideInInspector] public int OldBundleVersionCode;

        [Header("Current Build")]
        [DisableEditing] public string SemVer;
        [Tooltip("Android only")]
        [DisableEditing] public int AndroidBundleVersionCode;

        [Header("Last Successful Build")]
        [Tooltip("SemVer of the last successful build. The next build gets this version plus one.")]
        [DisableEditing] public string LastSuccessfulSemVer;
        [Tooltip("Android bundle version code of the last successful build. The next build gets this code plus one.")]
        [DisableEditing] public int LastSuccessfulBundleVersionCode;

        [Header("Previous Builds")]
        [DisableEditing] public int TotalAttemptedBuilds;
        [DisableEditing] public int TotalSuccessBuilds;
        [DisableEditing] public BuildResult LastBuildResult = BuildResult.Unknown;

        /// <summary>
        /// Exposes the configured path for the build info CSV file.
        /// Creates the directory if it does not exist.
        /// </summary>
        public string FilePath
        {
            get
            {
                if (!Directory.Exists(Path.GetDirectoryName(m_filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(m_filePath) ?? string.Empty);
                }

                if (!File.Exists(m_filePath))
                {
                    Debug.LogWarningFormat("File does not exist: {0}. It should be created upon your first build.", m_filePath);
                }

                return m_filePath;
            }
        }


        [ContextMenu(nameof(IncreaseAttemptedBuilds))]
        private void IncreaseAttemptedBuilds()
        {
            TotalAttemptedBuilds++;
        }


        [ContextMenu(nameof(IncreaseSuccessfulBuilds))]
        private void IncreaseSuccessfulBuilds()
        {
            TotalSuccessBuilds++;
        }
    }
}
