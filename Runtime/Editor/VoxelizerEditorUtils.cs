using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Voxelization.Tools
{
    public static class VoxelizerEditorUtils
    {
        public static Color GetColorFromHEX(string colorCode, float alfa = 1f)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(colorCode, out color))
            {
                color.a = alfa;
                return color;
            }
            return Color.white;
        }

        public static void HorizontalLine(Color color)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static void DrawUIBox(Color borderColor, Color backgroundColor, Rect rect, int width = 2)
        {
            borderColor.a = 0.25f;
            Rect outter = new Rect(rect);
            Rect inner = new Rect(rect.x + width, rect.y + width, rect.width - width * 2, rect.height - width * 2);
            EditorGUI.DrawRect(outter, borderColor);
            EditorGUI.DrawRect(inner, backgroundColor);
        }

        public static bool PlaceErrorBox(string msg, bool isError)
        {
            if (isError)
            {
                return isError;
            }
            isError = true;

            PlaceHelpBox(msg, MessageType.Error);

            return isError;
        }

        public static void PlaceHelpBox(string msg, MessageType messageType)
        {
            EditorGUILayout.HelpBox(msg, messageType);
            EditorGUILayout.Space(3);
        }
    }
}
