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

            if (GUILayout.Button("Fill path references"))
            {
                foreach (MagicLibrary l_magicLibrary in m_magicLibraries.m_magicPaths)
                {
                    Dictionary<string, SpellPath> l_spellPaths = l_magicLibrary.paths.ToDictionary(p_spellPath => p_spellPath.name, p_spellPath => p_spellPath);
                    foreach (SpellPath l_spellPath in l_magicLibrary.paths)
                    {
                        Debug.Log($"{l_magicLibrary.name} - {l_spellPath.name}\n{string.Join(", ", l_spellPath.opposedPaths)}");
                        l_spellPath.OpposedPath = l_spellPath.opposedPaths.Select(p_path => l_spellPaths[p_path]).ToArray();

                        foreach (Spell l_spell in l_spellPath.spells)
                        {
                            l_spell.PathReference = l_spellPath;
                            l_spell.ForbiddenPaths = l_spellPath.opposedPaths.Select(p_path => l_spellPaths[p_path]).ToArray();
                        }
                    }
                }
            }

            if (GUILayout.Button("Reset player prefs"))
            {
                PathSelection l_pathSelection = new PathSelection();
                l_pathSelection.m_pathSelection = new bool[m_magicLibraries.m_magicPaths.Length][];
                for (int i = 0; i < l_pathSelection.m_pathSelection.Length; i++)
                    l_pathSelection.m_pathSelection[i] = new bool[m_magicLibraries.m_magicPaths[i].paths.Length];

                foreach (bool[] l_path in l_pathSelection.m_pathSelection)
                    for (int l_index = 0; l_index < l_path.Length; l_index++)
                        l_path[l_index] = true;

                PlayerPrefs.SetString("path_selection", l_pathSelection.ToString());
            }
        }
    }
}
