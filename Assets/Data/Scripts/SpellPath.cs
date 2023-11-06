using System;

namespace Data.Scripts
{
	[Serializable]
    public class SpellPath
    {
        public string name;
        public PathType pathType;
        public string[] opposedPaths;
        public string description;
        public Spell[] spells;

        private SpellPath[] m_opposedPath;

        public SpellPath[] OpposedPath
        {
            get => m_opposedPath;
            set => m_opposedPath = value;
        }
    }
}
