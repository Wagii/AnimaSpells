using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Scripts;
using JetBrains.Annotations;
using Scriptables;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public class MagicLibrariesEditor : EditorWindow
	{
		private static bool s_displayEditor = false;
		private static MagicLibraries s_magicLibraries;
		private static readonly Dictionary<string, List<SpellPath>> MagicLibrariesDict = new Dictionary<string, List<SpellPath>>();
		private static readonly Dictionary<MagicLibrary, bool> DisplayGraphics = new Dictionary<MagicLibrary, bool>();
		private static Vector2 s_scroll;

		[MenuItem("Tools/PathGraphics")]
		public static void ShowWindow()
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MagicLibraries").First());
			s_magicLibraries = AssetDatabase.LoadAssetAtPath<MagicLibraries>(assetPath);

			SpellPath[] paths = s_magicLibraries.m_magicPaths.SelectMany(p_library => p_library.paths).ToArray();
			foreach (SpellPath path in paths)
			{
				if (MagicLibrariesDict.TryGetValue(path.name, out List<SpellPath> l_value))
					l_value.Add(path);
				else
					MagicLibrariesDict.Add(path.name, new List<SpellPath> { path });
			}

			foreach (SpellPath l_spellPath in paths)
			{
				if (s_magicLibraries.m_PathGraphicsArray.None(p_graphics => p_graphics.pathReference == l_spellPath.name))
					s_magicLibraries.m_PathGraphicsArray = s_magicLibraries.m_PathGraphicsArray.Append(new PathGraphics(l_spellPath.name)).ToArray();
			}

			s_magicLibraries.PathGraphicsMap.Clear();
			foreach (SpellPath l_spellPath in paths)
			{
				s_magicLibraries.PathGraphicsMap.TryAdd(l_spellPath.name, s_magicLibraries.m_PathGraphicsArray.First(p_path => p_path.pathReference == l_spellPath.name));
			}

			DisplayGraphics.Clear();
			foreach (MagicLibrary l_magicLibrary in s_magicLibraries.m_magicPaths)
				DisplayGraphics.Add(l_magicLibrary, false);

			MagicLibrariesEditor l_editor = GetWindow<MagicLibrariesEditor>();
			l_editor.titleContent = new GUIContent("Magic library editor");
			l_editor.Show();
		}

		private void OnGUI()
		{
			s_scroll = EditorGUILayout.BeginScrollView(s_scroll);
			EditorGUI.indentLevel++;

			foreach (MagicLibrary l_magicLibrary in s_magicLibraries.m_magicPaths)
			{
				DisplayGraphics[l_magicLibrary] = EditorGUILayout.Foldout(DisplayGraphics[l_magicLibrary], l_magicLibrary.name);
				if (DisplayGraphics[l_magicLibrary])
				{
					EditorGUI.indentLevel++;
					foreach (SpellPath l_spellPath in l_magicLibrary.paths)
					{
						EditorGUILayout.BeginHorizontal();
						PathGraphics pathGraphic = s_magicLibraries.PathGraphicsMap[l_spellPath.name];
						EditorGUILayout.ColorField(l_spellPath.name, pathGraphic.color);
						EditorGUILayout.ObjectField(pathGraphic.texture2D, typeof(Texture2D));
						EditorGUILayout.EndHorizontal();
					}
					EditorGUI.indentLevel--;
				}
			}

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

				s_magicLibraries.m_magicPaths = l_magicLibraries.ToArray();
				AssetDatabase.SaveAssets();
			}

			if (GUILayout.Button("Fill path references"))
			{
				foreach (MagicLibrary l_magicLibrary in s_magicLibraries.m_magicPaths)
				{
					Dictionary<string, SpellPath> l_spellPaths = l_magicLibrary.paths.ToDictionary(p_spellPath => p_spellPath.name, p_spellPath => p_spellPath);
					foreach (SpellPath l_spellPath in l_magicLibrary.paths)
					{
						Debug.Log($"{l_magicLibrary.name} - {l_spellPath.name}\n{string.Join(", ", l_spellPath.opposedPaths)}");
						l_spellPath.OpposedPath = l_spellPath.opposedPaths.Select(p_path => l_spellPaths[p_path]).ToArray();

						foreach (Spell l_spell in l_spellPath.spells)
							l_spell.PathReference = l_spellPath;
					}
				}
			}

			if (GUILayout.Button("Reset player prefs"))
			{
				PathSelection l_pathSelection = new PathSelection();
				l_pathSelection.m_pathSelection = new bool[s_magicLibraries.m_magicPaths.Length][];
				for (int i = 0; i < l_pathSelection.m_pathSelection.Length; i++)
					l_pathSelection.m_pathSelection[i] = new bool[s_magicLibraries.m_magicPaths[i].paths.Length];

				foreach (bool[] l_path in l_pathSelection.m_pathSelection)
					for (int l_index = 0; l_index < l_path.Length; l_index++)
						l_path[l_index] = true;

				PlayerPrefs.SetString("path_selection", l_pathSelection.ToString());
			}

			EditorGUILayout.Separator();

			foreach (KeyValuePair<string,List<SpellPath>> valuePair in MagicLibrariesDict)
			{
				EditorGUILayout.BeginHorizontal();
				PathGraphics pathGraphic = s_magicLibraries.PathGraphicsMap[valuePair.Key];
				pathGraphic.color = EditorGUILayout.ColorField(valuePair.Key, pathGraphic.color);
				pathGraphic.texture2D = EditorGUILayout.ObjectField(pathGraphic.texture2D, typeof(Texture2D), true) as Texture2D;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("Extract highest color"))
			{
				foreach (KeyValuePair<string, List<SpellPath>> valuePair in MagicLibrariesDict)
				{
					PathGraphics l_pathGraphics = s_magicLibraries.PathGraphicsMap[valuePair.Key];
					Texture2D texture = l_pathGraphics.texture2D;
					if (texture == null) continue;

					// Color32 color = texture.GetPixels32().Where(p_color => p_color.a != 0).Select(p_color => Color.RGBToHSV(p_color, out float hue, out float saturation, out float vibrance)).OrderByDescending(p_color => p_color.GetVibrance()).ThenByDescending(p_color => p_color.GetSaturation()).First();
					Color selectedColor = texture.GetDominantColor();
					Color.RGBToHSV(selectedColor, out float H, out float S, out float V);
					Debug.Log($"{valuePair.Key} : {H} {S} {V}");
					selectedColor.a = 1;

					l_pathGraphics.color = selectedColor;
				}

				AssetDatabase.SaveAssets();
			}

			if (GUILayout.Button("Extract median color"))
			{
				foreach (PathGraphics l_pathGraphics in s_magicLibraries.m_PathGraphicsArray)
				{
					if (l_pathGraphics.texture2D == null) continue;
					Color[] l_pixels = l_pathGraphics.texture2D.GetPixels();
					l_pixels = l_pixels.Where(p_pixel => Math.Abs(p_pixel.a - 1) < 0.01f).ToArray();
					Color l_medianColor = l_pixels.Select(p_color => p_color / l_pixels.Length).Sum();
					l_medianColor.a = 1;
					Color.RGBToHSV(l_medianColor, out float h, out float s, out float v);
					v /= .6f;
					l_medianColor = Color.HSVToRGB(h, s, v);
					l_pathGraphics.color = l_medianColor;
				}
			}

			s_displayEditor = EditorGUILayout.Toggle("Display default editor", s_displayEditor);
			if (s_displayEditor)
			{
				UnityEditor.Editor l_editor = UnityEditor.Editor.CreateEditor(s_magicLibraries);
				l_editor.OnInspectorGUI();
			}

			EditorGUI.indentLevel--;
			EditorGUILayout.EndScrollView();
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

	public static bool None<TSource>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate)
		=> !source.Any(predicate);

	public static bool None<TSource>([NotNull] this IEnumerable<TSource> source)
		=> !source.Any();

	public static Color Sum(this IEnumerable<Color> colors)
		=> colors.Aggregate(Color.clear, (p_current, p_color) => p_current + p_color);
}
