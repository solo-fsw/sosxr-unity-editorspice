using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Editor window that lists the project's Git (and optionally registry) packages, checks them for updates,
    ///     and updates all selected packages in one Package Manager request (one resolve, one domain reload).
    ///     Git packages are checked with `git ls-remote`. Git must be on the PATH, as for any UPM Git dependency.
    /// </summary>
    public class BatchPackageUpdater : EditorWindow
    {
        private const int GitTimeoutMilliseconds = 20000;

        private static readonly Color UpdateColor = new(1f, 0.75f, 0.2f);
        private static readonly Color UpToDateColor = new(0.45f, 0.85f, 0.45f);
        private static readonly Color ErrorColor = new(1f, 0.45f, 0.45f);

        [SerializeField] private string _filter = string.Empty;
        [SerializeField] private bool _showRegistryPackages;

        private readonly List<Entry> _entries = new();
        private readonly object _repaintLock = new();

        private ListRequest _listRequest;
        private AddAndRemoveRequest _updateRequest;
        private string _status = string.Empty;
        private bool _needsRepaint;
        private Vector2 _scrollPosition;

        /// <summary>
        ///     The packages the window shows (after the last refresh), before filtering.
        /// </summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        ///     True while the window lists packages or runs an update.
        /// </summary>
        public bool IsBusy => _listRequest != null || _updateRequest != null;


        [MenuItem("SOSXR/Batch Package Updater")]
        public static BatchPackageUpdater ShowWindow()
        {
            var window = GetWindow<BatchPackageUpdater>("Package Updater");
            window.minSize = new Vector2(480, 300);

            return window;
        }


        private void OnEnable()
        {
            EditorApplication.update += Poll;
            Refresh();
        }


        private void OnDisable()
        {
            EditorApplication.update -= Poll;
        }


        /// <summary>
        ///     Lists the installed packages again and starts a new update check.
        /// </summary>
        public void Refresh()
        {
            if (IsBusy)
            {
                return;
            }

            _status = "Listing packages…";
            _listRequest = Client.List(false, false);
        }


        private void Poll()
        {
            if (_listRequest is {IsCompleted: true})
            {
                OnListCompleted(_listRequest);
                _listRequest = null;
                Repaint();
            }

            if (_updateRequest is {IsCompleted: true})
            {
                OnUpdateCompleted(_updateRequest);
                _updateRequest = null;
                Refresh();
                Repaint();
            }

            lock (_repaintLock)
            {
                if (!_needsRepaint)
                {
                    return;
                }

                _needsRepaint = false;
            }

            Repaint();
        }


        private void OnListCompleted(ListRequest request)
        {
            var previouslySelected = new HashSet<string>(_entries.Where(e => e.Selected).Select(e => e.Name));
            _entries.Clear();

            if (request.Status != StatusCode.Success)
            {
                _status = $"Could not list packages: {request.Error?.message}";

                return;
            }

            foreach (var info in request.Result)
            {
                if (!info.isDirectDependency)
                {
                    continue;
                }

                var entry = CreateEntry(info);

                if (entry == null)
                {
                    continue;
                }

                entry.Selected = previouslySelected.Contains(entry.Name);
                _entries.Add(entry);
            }

            _entries.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            _status = string.Empty;

            foreach (var entry in _entries.Where(e => e.State == UpdateState.Checking))
            {
                StartGitCheck(entry);
            }
        }


        private static Entry CreateEntry(PackageInfo info)
        {
            var entry = new Entry
            {
                Name = info.name,
                DisplayName = string.IsNullOrEmpty(info.displayName) ? info.name : info.displayName,
                Version = info.version,
                Source = info.source
            };

            switch (info.source)
            {
                case PackageSource.Git:
                    var gitUrl = PackageUpdateMath.GetSourceFromPackageId(info.packageId);
                    entry.AddIdentifier = gitUrl;
                    entry.RemoteUrl = PackageUpdateMath.GetRemoteUrl(gitUrl);
                    entry.Revision = PackageUpdateMath.GetRevision(gitUrl);
                    entry.InstalledHash = info.git?.hash;

                    if (PackageUpdateMath.IsCommitHash(entry.Revision))
                    {
                        entry.State = UpdateState.Pinned;
                        entry.Message = "Pinned to a commit";
                    }
                    else
                    {
                        entry.State = UpdateState.Checking;
                        entry.Message = "Checking…";
                    }

                    return entry;

                case PackageSource.Registry:
                    var latest = info.versions?.latestCompatible;
                    entry.LatestVersion = latest;
                    entry.AddIdentifier = string.IsNullOrEmpty(latest) ? null : $"{info.name}@{latest}";

                    if (PackageUpdateMath.HasRegistryUpdate(info.version, latest))
                    {
                        entry.State = UpdateState.UpdateAvailable;
                        entry.Message = $"{info.version} → {latest}";
                    }
                    else
                    {
                        entry.State = UpdateState.UpToDate;
                        entry.Message = "Up to date";
                    }

                    return entry;

                default:
                    return null; // Embedded, local, built-in and tarball packages can't be updated from here
            }
        }


        private void StartGitCheck(Entry entry)
        {
            var remote = entry.RemoteUrl;
            var revision = string.IsNullOrEmpty(entry.Revision) ? "HEAD" : entry.Revision;

            Task.Run(() =>
            {
                var output = RunGitLsRemote(remote, revision, out var error);
                var remoteHash = PackageUpdateMath.ParseLsRemoteHash(output);

                if (remoteHash == null)
                {
                    entry.Message = string.IsNullOrEmpty(error) ? $"Revision '{revision}' not found on remote" : error;
                    entry.State = UpdateState.Unknown;
                }
                else if (PackageUpdateMath.IsSameCommit(entry.InstalledHash, remoteHash))
                {
                    entry.Message = "Up to date";
                    entry.State = UpdateState.UpToDate;
                }
                else
                {
                    entry.Message = $"{PackageUpdateMath.ShortHash(entry.InstalledHash)} → {PackageUpdateMath.ShortHash(remoteHash)}";
                    entry.State = UpdateState.UpdateAvailable;
                }

                lock (_repaintLock)
                {
                    _needsRepaint = true;
                }
            });
        }


        private static string RunGitLsRemote(string remote, string revision, out string error)
        {
            error = null;

            var startInfo = new ProcessStartInfo("git", $"ls-remote \"{remote}\" \"{revision}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0"; // Fail instead of waiting for a password

            if (!startInfo.EnvironmentVariables.ContainsKey("GIT_SSH_COMMAND"))
            {
                startInfo.EnvironmentVariables["GIT_SSH_COMMAND"] = "ssh -o BatchMode=yes";
            }

            try
            {
                using var process = Process.Start(startInfo);

                if (process == null)
                {
                    error = "Could not start git";

                    return null;
                }

                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(GitTimeoutMilliseconds))
                {
                    process.Kill();
                    error = "git ls-remote timed out";

                    return null;
                }

                if (process.ExitCode != 0)
                {
                    error = FirstLine(stderr.Result);

                    return null;
                }

                return stdout.Result;
            }
            catch (Exception exception)
            {
                error = $"git failed: {exception.Message}";

                return null;
            }
        }


        private static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "git ls-remote failed";
            }

            return text.Trim().Split('\n')[0].Trim();
        }


        /// <summary>
        ///     Updates all selected packages in one Package Manager request.
        ///     Git packages are added again with their manifest URL, so UPM fetches the newest commit of their revision.
        /// </summary>
        public void UpdateSelected()
        {
            if (IsBusy)
            {
                return;
            }

            var selected = _entries.Where(e => e.Selected && e.CanUpdate).ToList();

            if (selected.Count == 0)
            {
                return;
            }

            _status = $"Updating {selected.Count} package(s)…";
            _updateRequest = Client.AddAndRemove(selected.Select(e => e.AddIdentifier).ToArray());

            Debug.Log($"[BatchPackageUpdater] Updating: {string.Join(", ", selected.Select(e => e.Name))}");
        }


        private void OnUpdateCompleted(AddAndRemoveRequest request)
        {
            if (request.Status == StatusCode.Success)
            {
                _status = "Update finished";
                Debug.Log("[BatchPackageUpdater] Update finished.");

                foreach (var entry in _entries)
                {
                    entry.Selected = false;
                }
            }
            else
            {
                _status = $"Update failed: {request.Error?.message}";
                Debug.LogError($"[BatchPackageUpdater] {_status}");
            }
        }


        private void OnGUI()
        {
            DrawToolbar();
            DrawSelectionButtons();
            DrawList();
            DrawFooter();
        }


        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(IsBusy))
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        Refresh();
                    }
                }

                _showRegistryPackages = GUILayout.Toggle(_showRegistryPackages, "Registry packages", EditorStyles.toolbarButton, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MaxWidth(220));
            }
        }


        private void DrawSelectionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Select:", GUILayout.Width(45));

                if (GUILayout.Button("Updates", EditorStyles.miniButtonLeft))
                {
                    foreach (var entry in VisibleEntries())
                    {
                        entry.Selected = entry.State == UpdateState.UpdateAvailable;
                    }
                }

                if (GUILayout.Button("All", EditorStyles.miniButtonMid))
                {
                    foreach (var entry in VisibleEntries())
                    {
                        entry.Selected = entry.CanUpdate;
                    }
                }

                if (GUILayout.Button("None", EditorStyles.miniButtonRight))
                {
                    foreach (var entry in _entries)
                    {
                        entry.Selected = false;
                    }
                }
            }
        }


        private void DrawList()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;

            var visible = VisibleEntries().ToList();

            if (visible.Count == 0 && !IsBusy)
            {
                EditorGUILayout.HelpBox("No Git packages in this project's manifest match the filter.", MessageType.Info);

                return;
            }

            foreach (var entry in visible)
            {
                DrawEntry(entry);
            }
        }


        private static void DrawEntry(Entry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!entry.CanUpdate))
                {
                    entry.Selected = EditorGUILayout.Toggle(entry.Selected && entry.CanUpdate, GUILayout.Width(18));
                }

                var label = new GUIContent(entry.DisplayName, $"{entry.Name}\n{entry.AddIdentifier}");
                EditorGUILayout.LabelField(label, GUILayout.MinWidth(120));

                var versionText = entry.Source == PackageSource.Git ? $"{entry.Version} ({PackageUpdateMath.ShortHash(entry.InstalledHash)})" : entry.Version;
                EditorGUILayout.LabelField(versionText, EditorStyles.miniLabel, GUILayout.Width(120));

                var previousColor = GUI.contentColor;
                GUI.contentColor = StateColor(entry.State, previousColor);
                EditorGUILayout.LabelField(new GUIContent(entry.Message, entry.Message), EditorStyles.miniLabel, GUILayout.MinWidth(120));
                GUI.contentColor = previousColor;
            }
        }


        private void DrawFooter()
        {
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.LabelField(_status, EditorStyles.miniLabel);
            }

            var count = _entries.Count(e => e.Selected && e.CanUpdate);

            using (new EditorGUI.DisabledScope(IsBusy || count == 0 || EditorApplication.isCompiling))
            {
                if (GUILayout.Button($"Update selected ({count})", GUILayout.Height(28)))
                {
                    UpdateSelected();
                }
            }
        }


        private IEnumerable<Entry> VisibleEntries()
        {
            foreach (var entry in _entries)
            {
                if (entry.Source == PackageSource.Registry && !_showRegistryPackages)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(_filter) &&
                    entry.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                    entry.DisplayName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                yield return entry;
            }
        }


        private static Color StateColor(UpdateState state, Color fallback)
        {
            return state switch
                   {
                       UpdateState.UpdateAvailable => UpdateColor,
                       UpdateState.UpToDate => UpToDateColor,
                       UpdateState.Unknown => ErrorColor,
                       _ => fallback
                   };
        }


        public enum UpdateState
        {
            Checking,
            UpToDate,
            UpdateAvailable,
            Pinned,
            Unknown
        }


        /// <summary>
        ///     One package row. Git check results are written from a background thread; State is set last.
        /// </summary>
        public class Entry
        {
            public string Name;
            public string DisplayName;
            public string Version;
            public PackageSource Source;
            public string AddIdentifier;
            public string RemoteUrl;
            public string Revision;
            public string InstalledHash;
            public string LatestVersion;
            public volatile string Message;
            public volatile UpdateState State;
            public bool Selected;

            /// <summary>
            ///     Pinned Git packages never change, so they can't be selected. Other packages can, even when the check failed.
            /// </summary>
            public bool CanUpdate => State != UpdateState.Pinned && !string.IsNullOrEmpty(AddIdentifier);
        }
    }
}
