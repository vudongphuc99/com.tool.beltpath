using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Tool.BeltPath.Editor
{
    /// <summary>Shows the installed and latest BeltPath version and updates the project's git tag with one click.</summary>
    public sealed class PackageUpdaterWindow : EditorWindow
    {
        private InstalledPackage _installed;
        private bool _hasInstalled;
        private Task<GitTagResult> _check;
        private GitTagResult _latest;
        private bool _hasLatest;
        private AddRequest _update;
        private string _message;

        [MenuItem("BeltPath/Package Updater")]
        public static void Open()
        {
            GetWindow<PackageUpdaterWindow>("BeltPath Updater");
        }

        private void OnEnable()
        {
            EditorApplication.update += HandleEditorUpdate;
            StartCheck();
        }

        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
        }

        private void OnGUI()
        {
            DrawVersions();
            DrawStatus();
            DrawButtons();
        }

        private void DrawVersions()
        {
            EditorGUILayout.LabelField("Installed", _hasInstalled ? $"{_installed.Version} ({_installed.Source})" : "not found");
            EditorGUILayout.LabelField("Latest", LatestLabel());
        }

        private string LatestLabel()
        {
            if (_check != null)
            {
                return "checking...";
            }

            return _hasLatest && _latest.Success ? _latest.Tag : "unknown";
        }

        private void DrawStatus()
        {
            if (_hasLatest && !_latest.Success)
            {
                EditorGUILayout.HelpBox(_latest.Error, MessageType.Error);
            }

            if (_hasInstalled && _installed.Source != PackageSource.Git)
            {
                EditorGUILayout.HelpBox(
                    "This project does not install the package from git (embedded or local), so it cannot be updated here.",
                    MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.None);
            }
        }

        private void DrawButtons()
        {
            using (new EditorGUI.DisabledScope(_check != null || _update != null))
            {
                if (GUILayout.Button("Check For Updates"))
                {
                    StartCheck();
                }
            }

            using (new EditorGUI.DisabledScope(!CanUpdate()))
            {
                if (GUILayout.Button(UpdateLabel()))
                {
                    StartUpdate();
                }
            }
        }

        private string UpdateLabel()
        {
            return CanUpdate() ? $"Update to {_latest.Tag}" : "Up to date";
        }

        private bool CanUpdate()
        {
            return _hasInstalled
                   && _installed.Source == PackageSource.Git
                   && _hasLatest
                   && _latest.Success
                   && _latest.Version > _installed.Version
                   && _check == null
                   && _update == null;
        }

        private void StartCheck()
        {
            _message = null;
            _hasInstalled = InstalledPackageReader.TryRead(out _installed);
            if (!_hasInstalled)
            {
                _message = "The BeltPath package is not installed in this project.";
                return;
            }

            _check = GitTagReader.ReadLatestAsync(_installed.GitUrl);
        }

        private void StartUpdate()
        {
            _message = $"Updating to {_latest.Tag}...";
            _update = Client.Add($"{_installed.GitUrl}#{_latest.Tag}");
        }

        private void HandleEditorUpdate()
        {
            FinishCheckIfDone();
            FinishUpdateIfDone();
        }

        private void FinishCheckIfDone()
        {
            if (_check == null || !_check.IsCompleted)
            {
                return;
            }

            _latest = _check.Result;
            _hasLatest = true;
            _check = null;
            Repaint();
        }

        private void FinishUpdateIfDone()
        {
            if (_update == null || !_update.IsCompleted)
            {
                return;
            }

            _message = _update.Status == StatusCode.Success
                ? $"Updated to {_latest.Tag}."
                : $"Update failed: {_update.Error?.message}";
            _update = null;
            _hasInstalled = InstalledPackageReader.TryRead(out _installed);
            Repaint();
        }
    }
}
