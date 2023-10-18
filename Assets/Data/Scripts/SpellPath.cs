using System;
using UnityEngine;
using UnityEngine.UI;

namespace Data.Scripts
{
    [Serializable]
    public class SpellPath
    {
        public string name;
        public PathType pathType;
        public string[] opposedPaths;
        public string description;
        public Color pathColor;
        public Image pathImage;
        public Spell[] spells;
    }
}
