using System.Linq;
using Scriptables;
using UnityEngine;

public class PathSelection
{
    public bool[][] m_PathSelection;

    public readonly string m_SaveKey = "path_selection";

    public override string ToString()
    {
        string l_string = "";
        for (int l_index = 0; l_index < m_PathSelection.Length; l_index++)
        {
            bool[] l_bools = m_PathSelection[l_index];
            l_string += string.Join(",", l_bools.Select(p_bool => p_bool ? "1" : "0"));
            if (l_index != m_PathSelection.Length - 1)
                l_string += "|";
        }

        return l_string;
    }

    public PathSelection() {}

    public PathSelection(string p_string, string p_saveKey = "path_selection")
    {
        string[] l_lines = p_string.Split('|');
        m_PathSelection = new bool[l_lines.Length][];
        for (int l_index = 0; l_index < l_lines.Length; l_index++)
        {
            string l_line = l_lines[l_index];
            string[] l_bools = l_line.Split(',');
            m_PathSelection[l_index] = new bool[l_bools.Length];
            for (int l_boolIndex = 0; l_boolIndex < l_bools.Length; l_boolIndex++)
            {
                string l_bool = l_bools[l_boolIndex];
                m_PathSelection[l_index][l_boolIndex] = l_bool == "1";
            }
        }
        m_SaveKey = p_saveKey;
    }

    public void SaveSelection()
    {
        PlayerPrefs.SetString(m_SaveKey, ToString());
        PlayerPrefs.Save();
    }

    public static PathSelection LoadSelection(string p_saveKey = "path_selection")
    {
        if (!PlayerPrefs.HasKey(p_saveKey) || PlayerPrefs.GetString(p_saveKey, "") == "")
            GenerateDefaultPathSelection(p_saveKey);
        return new PathSelection(PlayerPrefs.GetString(p_saveKey, ""), p_saveKey);
    }

    public static void GenerateDefaultPathSelection(string p_saveKey)
    {
        if (Displayer.m_Instance == null || Displayer.MagicLibraries == null) return;

        MagicLibraries magicLibraries = Displayer.MagicLibraries;
        PathSelection pathSelection = new PathSelection();

        int libraryCount = magicLibraries.m_magicPaths.Length;
        pathSelection.m_PathSelection = new bool[libraryCount][];
        for (int i = 0; i < libraryCount; i++)
            pathSelection.m_PathSelection[i] = new bool[magicLibraries.m_magicPaths[i].paths.Length];

        PlayerPrefs.SetString(p_saveKey, pathSelection.ToString());
    }
}
