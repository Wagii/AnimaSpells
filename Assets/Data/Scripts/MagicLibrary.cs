using System;
using System.Collections.Generic;

namespace Data.Scripts
{
    [Serializable]
    public class MagicLibrary
    {
        public string name;
        public SpellPath[] paths;

        public MagicLibrary() { }
    }
}
