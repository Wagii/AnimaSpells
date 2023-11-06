using UnityEngine;

namespace Scriptables
{
    [System.Serializable]
    public class PathGraphics
    {
        public string pathReference;
        public Color color;
        public Texture2D texture2D;

        public PathGraphics() { }

        public PathGraphics(string p_pathReference)
        {
            pathReference = p_pathReference;
        }

        public PathGraphics(string p_pathReference, Color p_color, Texture2D p_texture2D)
        {
            pathReference = p_pathReference;
            color = p_color;
            texture2D = p_texture2D;
        }
    }
}
