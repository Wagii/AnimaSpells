using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Scripts;
using Scriptables;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(MagicLibraries))]
    public class MagicLibrariesEditor : UnityEditor.Editor
    {
        private MagicLibraries m_magicLibraries;

        private void OnEnable()
        {
            m_magicLibraries = (MagicLibraries) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Separator();
            if (GUILayout.Button("Update"))
            {
                List<MagicLibrary> l_magicLibraries = new List<MagicLibrary>();
                string[] l_assets = AssetDatabase.FindAssets("t:TextAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p_path => p_path.Contains("Assets/"))
                    .Where(p_path => Path.GetExtension(p_path).Contains("json"))
                    .ToArray();

                foreach (string l_asset in l_assets)
                {
                    try
                    {
                        string l_text = File.ReadAllText(l_asset);
                        MagicLibrary l_magicLibrary = JsonUtility.FromJson<MagicLibrary>(l_text);
                        l_magicLibraries.Add(l_magicLibrary);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error while loading {l_asset}: {e.Message}");
                    }
                }

                m_magicLibraries.m_magicPaths = l_magicLibraries.ToArray();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
