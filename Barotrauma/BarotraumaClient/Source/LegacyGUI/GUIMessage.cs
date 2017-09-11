﻿using Microsoft.Xna.Framework;

namespace Barotrauma
{
    namespace LegacyGUI
    {
        class GUIMessage
        {
            private ColoredText coloredText;
            private Vector2 pos;

            private float lifeTime;

            private Vector2 size;

            public string Text
            {
                get { return coloredText.Text; }
            }

            public Color Color
            {
                get { return coloredText.Color; }
            }

            public Vector2 Pos
            {
                get { return pos; }
                set { pos = value; }
            }

            public Vector2 Size
            {
                get { return size; }
            }


            public float LifeTime
            {
                get { return lifeTime; }
                set { lifeTime = value; }
            }

            public GUIMessage(string text, Color color, Vector2 position, float lifeTime)
            {
                coloredText = new ColoredText(text, color);
                pos = position;
                this.lifeTime = lifeTime;

                size = GUI.Font.MeasureString(text);
            }
        }
    }
}
