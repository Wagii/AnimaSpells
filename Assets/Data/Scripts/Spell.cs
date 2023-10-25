using System;

namespace Data.Scripts
{
    [Serializable]
    public class Spell
    {
        [Serializable]
        public class NewSystem
        {
            [Serializable]
            public class SpellRank
            {
                public Rank rank;
                public int requiredInt;
                public int cost;
                public int maintain;
                public MaintainType maintainType;
                public string effectValues;

                public SpellRank() { }

                public SpellRank(Rank p_spellRank)
                {
                    rank = p_spellRank;
                    requiredInt = 0;
                    cost = 0;
                    maintain = -1;
                    maintainType = MaintainType.Non;
                    effectValues = "";
                }

                public override string ToString()
                {
                    return $"{rank}\n{requiredInt}\n{cost}\n{maintainType switch {MaintainType.Non => "Non", MaintainType.Round => maintain.ToString(), MaintainType.Daily => maintain + " Quotidien", MaintainType.ImiterUnSort => "Spécial", _ => throw new ArgumentOutOfRangeException()}}\n{effectValues}";
                }
            }

            public string effect = "";
            public SpellRank initial = new(Rank.Initial);
            public SpellRank intermediaire = new(Rank.Intermédiaire);
            public SpellRank avance = new(Rank.Avancé);
            public SpellRank arcane = new(Rank.Arcane);
        }

        [Serializable]
        public class OldSystem
        {
            public int cost;
            public string effect;
            public string additionalEffect;
            public int maxCost;
            public int maintainDivider;
            public int maintainCost;
            public MaintainType maintainType;
        }

        public string name;
        public string pathReference;
        public PathType pathReferenceType;
        public string[] forbiddenPaths;
        public int level;
        public Action action;
        public SpellType[] spellTypes;
        public NewSystem newSystem;
        public OldSystem oldSystem;

        private SpellPath m_pathReference;
        private SpellPath[] m_forbiddenPaths;

        public SpellPath PathReference
        {
            get => m_pathReference;
            set => m_pathReference = value;
        }

        public SpellPath[] ForbiddenPaths
        {
            get => m_forbiddenPaths;
            set => m_forbiddenPaths = value;
        }
    }
}
