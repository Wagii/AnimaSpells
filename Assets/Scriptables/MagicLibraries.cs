using System.Collections.Generic;
using Data.Scripts;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "MagicLibraries", menuName = "Scriptables/MagicLibraries")]
    public class MagicLibraries : ScriptableObject
    {
        public MagicLibrary[] m_magicPaths;

        public PathGraphics[] m_PathGraphicsArray;

        public Dictionary<string, PathGraphics> PathGraphicsMap = new Dictionary<string, PathGraphics>();
    }
}
