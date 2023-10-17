using Data.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "MagicLibraries", menuName = "Scriptables/MagicLibraries")]
    public class MagicLibraries : ScriptableObject
    {
        public MagicLibrary m_defaultPaths;
        public MagicLibrary m_customPaths;
    }
}
