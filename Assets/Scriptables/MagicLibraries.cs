using Data.Scripts;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "MagicLibraries", menuName = "Scriptables/MagicLibraries")]
    public class MagicLibraries : ScriptableObject
    {
        public MagicLibrary[] m_magicPaths;
    }
}
