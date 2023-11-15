using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class LerpColorEditor : UnityEditor.EditorWindow
    {
        private static Color colorLeft, colorRight, colorResult;
        private static float lerpValue;

        [UnityEditor.MenuItem("Tools/Lerp Color")]
        public static void ShowWindow()
        {
            LerpColorEditor l_lerpColorEditor = GetWindow<LerpColorEditor>("Lerp Color");
            l_lerpColorEditor.name = "Lerp Color";
            l_lerpColorEditor.Show();
        }

        void OnGUI()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();

            colorLeft = EditorGUILayout.ColorField(colorLeft);
            lerpValue = EditorGUILayout.Slider(lerpValue, 0, 1);
            colorRight = EditorGUILayout.ColorField(colorRight);

            EditorGUILayout.EndHorizontal();

            colorResult = Color.Lerp(colorLeft, colorRight, lerpValue);

            // GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(colorLeft.ToString());
            EditorGUILayout.ColorField(colorResult);
            EditorGUILayout.LabelField(colorRight.ToString());
            EditorGUILayout.EndHorizontal();
            // GUI.enabled = true;

            EditorGUI.indentLevel--;
        }
    }
}
