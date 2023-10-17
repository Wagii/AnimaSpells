using System;
using UnityEngine;

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
        public Spell[] spells;
    }
}
