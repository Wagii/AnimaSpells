using System;
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
        private TextAsset m_defaultPaths;
        private TextAsset m_customPaths;

        private void OnEnable()
        {
            m_magicLibraries = (MagicLibraries) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Separator();
            m_defaultPaths = (TextAsset) EditorGUILayout.ObjectField("Default Paths", m_defaultPaths, typeof(TextAsset), false);
            m_customPaths = (TextAsset) EditorGUILayout.ObjectField("Custom Paths", m_customPaths, typeof(TextAsset), false);
            if (GUILayout.Button("Update"))
            {
                if (m_defaultPaths != null)
                {
                    m_magicLibraries.m_defaultPaths = JsonUtility.FromJson<MagicLibrary>(m_defaultPaths.text);
                }
                if (m_customPaths != null)
                {
                    m_magicLibraries.m_customPaths = JsonUtility.FromJson<MagicLibrary>(m_customPaths.text);
                }
            }
        }
    }
}
