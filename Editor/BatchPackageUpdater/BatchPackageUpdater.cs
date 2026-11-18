using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    ///     For a Git package with a newer commit, the window reads the version from that commit's package.json
    ///     with a shallow, blobless fetch into a temporary folder.
    /// </summary>
    public class BatchPackageUpdater : EditorWindow
    {
        private const int GitTimeoutMilliseconds = 20000;
        private const int GitFetchTimeoutMilliseconds = 60000;
        private const string PendingUpdatesKey = "SOSXR.BatchPackageUpdater.PendingUpdates";

        private static readonly Color UpdateColor = new(1f, 0.75f, 0.2f);
        private static readonly Color UpToDateColor = new(0.45f, 0.85f, 0.45f);
        private static readonly Color ErrorColor = new(1f, 0.45f, 0.45f);

        [SerializeField] private string _filter = string.Empty;
        [SerializeField] private bool _showRegistryPackages;

        private readonly List<Entry> _entries = new();
        private readonly object _repaintLock = new();

        // NonSerialized: hot reload would otherwise keep a request whose native operation is gone after a domain reload.
        // Such a request reports Failure with an empty error, even when the update worked.
        [NonSerialized] private ListRequest _listRequest;
        [NonSerialized] private AddAndRemoveRequest _updateRequest;
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

            VerifyPendingUpdates();

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
                    entry.PackageJsonPath = PackageUpdateMath.GetPackageJsonPath(gitUrl);
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
                var remoteHash = PackageUpdateMath.ParseLsRemoteHash(output, revision);

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
                    entry.RemoteHash = remoteHash;
                    entry.Message = $"{PackageUpdateMath.FormatGitUpdateMessage(entry.Version, null, entry.InstalledHash, remoteHash)}  (reading version…)";
                    entry.State = UpdateState.UpdateAvailable;
                    RequestRepaint();

                    var remoteVersion = ReadRemoteVersion(remote, revision, remoteHash, entry.PackageJsonPath);
                    entry.LatestVersion = remoteVersion;
                    entry.Message = PackageUpdateMath.FormatGitUpdateMessage(entry.Version, remoteVersion, entry.InstalledHash, remoteHash);
                }

                RequestRepaint();
            });
        }


        private void RequestRepaint()
        {
            lock (_repaintLock)
            {
                _needsRepaint = true;
            }
        }


        private static string RunGitLsRemote(string remote, string revision, out string error)
        {
            var patterns = string.Join(" ", PackageUpdateMath.GetLsRemotePatterns(revision).Select(p => $"\"{p}\""));

            return RunGit($"ls-remote \"{remote}\" {patterns}", null, GitTimeoutMilliseconds, out error);
        }


        /// <summary>
        ///     Reads the "version" from package.json at the remote commit. Fetches only that commit (depth 1, no blobs)
        ///     into a temporary repository, then reads the single file. Returns null when anything fails.
        /// </summary>
        private static string ReadRemoteVersion(string remote, string revision, string remoteHash, string packageJsonPath)
        {
            var folder = Path.Combine(Path.GetTempPath(), "SOSXR_BatchPackageUpdater", Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(folder);

                if (RunGit("init -q", folder, GitTimeoutMilliseconds, out _) == null ||
                    RunGit($"remote add origin \"{remote}\"", folder, GitTimeoutMilliseconds, out _) == null)
                {
                    return null;
                }

                // Fetch by hash first, so the version matches the checked commit. Some hosts refuse hash fetches: fall back to the revision name.
                const string fetchOptions = "fetch -q --depth 1 --filter=blob:none --no-tags origin";

                if (RunGit($"{fetchOptions} {remoteHash}", folder, GitFetchTimeoutMilliseconds, out _) == null &&
                    RunGit($"{fetchOptions} \"{revision}\"", folder, GitFetchTimeoutMilliseconds, out _) == null)
                {
                    return null;
                }

                var json = RunGit($"show \"FETCH_HEAD:{packageJsonPath}\"", folder, GitFetchTimeoutMilliseconds, out _);

                return PackageUpdateMath.ParsePackageJsonVersion(json);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                DeleteFolder(folder);
            }
        }


        private static void DeleteFolder(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    return;
                }

                foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal); // Git pack files are read-only on Windows
                }

                Directory.Delete(folder, true);
            }
            catch (Exception)
            {
                // A leftover temp folder is harmless
            }
        }


        /// <summary>
        ///     Runs git and returns its standard output, or null on failure (error then holds the first line of standard error).
        /// </summary>
        private static string RunGit(string arguments, string workingDirectory, int timeoutMilliseconds, out string error)
        {
            error = null;

            var startInfo = new ProcessStartInfo("git", arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

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

                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    process.Kill();
                    error = $"git {arguments.Split(' ')[0]} timed out";

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
                return "git failed";
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

            var pending = selected.Select(e => new KeyValuePair<string, string>(e.Name, e.Source == PackageSource.Git ? e.RemoteHash : e.LatestVersion));
            SessionState.SetString(PendingUpdatesKey, PackageUpdateMath.SerializePendingUpdates(pending)); // Survives the domain reload an update can cause

            _status = $"Updating {selected.Count} package(s)…";
            _updateRequest = Client.AddAndRemove(selected.Select(e => e.AddIdentifier).ToArray());

            Debug.Log($"[BatchPackageUpdater] Updating: {string.Join(", ", selected.Select(e => e.Name))}");
        }


        private void OnUpdateCompleted(AddAndRemoveRequest request)
        {
            var errorMessage = request.Error?.message;

            if (request.Status == StatusCode.Success || string.IsNullOrEmpty(errorMessage))
            {
                // The refresh after this compares the installed packages with the pending updates and reports the result
                _status = "Checking update result…";

                return;
            }

            SessionState.EraseString(PendingUpdatesKey);
            _status = $"Update failed: {errorMessage}";
            Debug.LogError($"[BatchPackageUpdater] {_status}");
        }


        /// <summary>
        ///     Compares the freshly listed packages with the updates started earlier (also before a domain reload) and logs the result.
        /// </summary>
        private void VerifyPendingUpdates()
        {
            var pending = PackageUpdateMath.ParsePendingUpdates(SessionState.GetString(PendingUpdatesKey, string.Empty));

            if (pending.Count == 0)
            {
                return;
            }

            SessionState.EraseString(PendingUpdatesKey);

            var updated = new List<string>();
            var notUpdated = new List<string>();

            foreach (var update in pending)
            {
                var name = update.Key;
                var entry = _entries.FirstOrDefault(e => e.Name == name);

                if (entry != null && PackageUpdateMath.IsUpdateApplied(entry.Source == PackageSource.Git, entry.Version, entry.InstalledHash, update.Value))
                {
                    updated.Add($"{name} {entry.Version}");
                }
                else
                {
                    notUpdated.Add(name);
                }
            }

            foreach (var entry in _entries)
            {
                entry.Selected = false;
            }

            if (notUpdated.Count == 0)
            {
                _status = $"Updated {updated.Count} package(s)";
                Debug.Log($"[BatchPackageUpdater] Updated: {string.Join(", ", updated)}");

                return;
            }

            _status = $"Not updated: {string.Join(", ", notUpdated)}";
            Debug.LogError($"[BatchPackageUpdater] {_status}. Updated: {(updated.Count == 0 ? "none" : string.Join(", ", updated))}. See the Console for Package Manager errors.");
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
            public string RemoteHash;
            public string LatestVersion;
            public string PackageJsonPath;
            public volatile string Message;
            public volatile UpdateState State;
            public bool Selected;

            /// <summary>
            ///     Pinned Git packages never change, so they can't be selected. Other Git packages can, even when the check failed.
            ///     Registry packages can only be selected when an update is available: adding "name@latestCompatible" to an up-to-date
            ///     package could downgrade one installed above that version.
            /// </summary>
            public bool CanUpdate => State != UpdateState.Pinned && !string.IsNullOrEmpty(AddIdentifier) && (Source != PackageSource.Registry || State == UpdateState.UpdateAvailable);
        }
    }
}
