using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxelization.Tools
{
    public static class VoxelizerEditorStyles
    {
        public static GUIStyle GetActiveControlsBackground()
        {
            var style = new GUIStyle();
            var color = VoxelizerEditorUtils.GetColorFromHEX("#ACADA8", 0.5f);
            style.normal.background = MakeTex(600, 100, color);
            return style;
        }

        public static GUIStyle GetTinyButtonStyle(int fontSize, int borderWidth)
        {
            var style = new GUIStyle();
            style.fontSize = fontSize;
            style.alignment = TextAnchor.MiddleCenter;
            style.border = new RectOffset(borderWidth, borderWidth, borderWidth, borderWidth);
            return style;
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }
}
