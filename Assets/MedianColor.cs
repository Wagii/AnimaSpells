using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MedianColor : MonoBehaviour
{
    public Color _color1;
    public Color _color2;
    public float _mixAmount = 0.5f;
}

[CustomEditor(typeof(MedianColor))]
public class MedianColorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MedianColor l_medianColor = target as MedianColor;
        EditorGUILayout.BeginHorizontal();
        l_medianColor._color1 = EditorGUILayout.ColorField(l_medianColor._color1);
        l_medianColor._mixAmount = EditorGUILayout.Slider(l_medianColor._mixAmount, 0, 1);
        l_medianColor._color2 = EditorGUILayout.ColorField(l_medianColor._color2);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.ColorField(Color.Lerp(l_medianColor._color1, l_medianColor._color2, l_medianColor._mixAmount));
    }
}
