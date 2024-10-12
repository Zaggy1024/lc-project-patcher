using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nomnom.LCProjectPatcher.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace Nomnom.LCProjectPatcher.Editor {
    public class AssetModificationBlocker: AssetModificationProcessor {
        private static bool _isExiting;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnLoad() {
            var runtimeSettings = ModuleUtility.GetPatcherRuntimeSettings();
            if (runtimeSettings.DisableAutomaticScriptableObjectReloading) {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                return;
            }
            
            _isExiting = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj) {
            var runtimeSettings = ModuleUtility.GetPatcherRuntimeSettings();
            if (runtimeSettings.DisableAutomaticScriptableObjectReloading) {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                return;
            }
            
            if (obj == PlayModeStateChange.EnteredEditMode) {
                _isExiting = true;
                AssetDatabase.SaveAssets();
            }
        }

        private static string[] OnWillSaveAssets(string[] paths) {
            var runtimeSettings = ModuleUtility.GetPatcherRuntimeSettings();
            if (runtimeSettings.DisableAutomaticScriptableObjectReloading) {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                return paths;
            }
            
            if (Application.isPlaying || _isExiting) {
                if (_isExiting) {
                    _isExiting = false;
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                }

                var list = new List<string>();
                var settings = ModuleUtility.GetPatcherSettings();
                var soPath = settings.GetBaseLethalCompanyPath();
                
                foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new string[] { soPath })) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("UnityEngine")) continue;
                    list.Add(path);
                }

                var allPaths = paths.Concat(list);

                AssetDatabase.StartAssetEditing();
                try {
                    foreach (var path in allPaths) {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        Resources.UnloadAsset(asset);
                    }
                } catch (Exception e) {
                    Debug.LogError(e);
                }
                finally {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.StartAssetEditing();
                try {
                    foreach (var path in allPaths) {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                } catch (Exception e) {
                    Debug.LogError(e);
                }
                finally {
                    AssetDatabase.StopAssetEditing();
                }

                return Array.Empty<string>();
            }
            
            return paths;
        }
    }
}
