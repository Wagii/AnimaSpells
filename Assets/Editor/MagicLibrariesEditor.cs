using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Scripts;
using Scriptables;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
	[CustomEditor(typeof(MagicLibraries))]
	public class MagicLibrariesEditor : UnityEditor.Editor
	{

		private MagicLibraries m_magicLibraries;
		private Dictionary<string, List<SpellPath>> m_magicLibrariesDict = new Dictionary<string, List<SpellPath>>();

		private void OnEnable()
		{
			m_magicLibraries = (MagicLibraries) target;
			SpellPath[] paths = m_magicLibraries.m_magicPaths.SelectMany(p_library => p_library.paths).ToArray();
            foreach (SpellPath path in paths)
            {
                if (m_magicLibrariesDict.ContainsKey(path.name))
					m_magicLibrariesDict[path.name].Add(path);
				else
					m_magicLibrariesDict.Add(path.name, new List<SpellPath>() { path });
            }
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

			EditorGUILayout.Separator();

			foreach (KeyValuePair<string,List<SpellPath>> valuePair in m_magicLibrariesDict)
			{
				EditorGUILayout.BeginHorizontal();
				valuePair.Value[0].pathColor = EditorGUILayout.ColorField(valuePair.Key, valuePair.Value[0].pathColor);
				for (int i = 1; i < valuePair.Value.Count; i++)
					valuePair.Value[i].pathColor = valuePair.Value[0].pathColor;

				valuePair.Value[0].pathImage = EditorGUILayout.ObjectField(valuePair.Value[0].pathImage, typeof(Texture2D), true) as Texture2D;
				for (int i = 1; i < valuePair.Value.Count; i++)
					valuePair.Value[i].pathImage = valuePair.Value[0].pathImage;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("Extract highest color"))
			{
				foreach (KeyValuePair<string, List<SpellPath>> valuePair in m_magicLibrariesDict)
				{
					Texture2D texture = valuePair.Value[0].pathImage;
					if (texture == null) continue;

					// Color32 color = texture.GetPixels32().Where(p_color => p_color.a != 0).Select(p_color => Color.RGBToHSV(p_color, out float hue, out float saturation, out float vibrance)).OrderByDescending(p_color => p_color.GetVibrance()).ThenByDescending(p_color => p_color.GetSaturation()).First();
					Color selectedColor = texture.GetDominantColor();
					Color.RGBToHSV(selectedColor, out float H, out float S, out float V);
					Debug.Log($"{valuePair.Key} : {H} {S} {V}");

					foreach (SpellPath spellPath in valuePair.Value)
						spellPath.pathColor = selectedColor;
				}
			}
		}
	}
}

public static partial class Utils
{
	public static Color GetDominantColor(this Texture2D image)
	{
		Color[] pixels = image.GetPixels();
		bool isGreyScale = IsGreyScale(pixels);

		if (isGreyScale)
		{
			return GetBrightestColor(pixels);
		}
		else
		{
			return GetMostVibrantColor(pixels);
		}
	}

	public static bool IsGreyScale(this Color[] pixels)
	{
		foreach (Color pixel in pixels)
		{
			if (pixel.r != pixel.g || pixel.g != pixel.b)
			{
				return false;
			}
		}
		return true;
	}

	public static Color GetBrightestColor(this Color[] pixels)
	{
		return pixels.OrderByDescending(c => c.grayscale).First();
	}

	public static Color GetMostVibrantColor(this Color[] pixels)
	{
		return pixels.OrderByDescending(c => c.r + c.g + c.b).First();
	}
}
