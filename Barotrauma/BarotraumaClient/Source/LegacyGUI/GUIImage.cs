﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Barotrauma
{
    namespace LegacyGUI
    {
        public class GUIImage : GUIComponent
        {
            public float Rotation;

            private Sprite sprite;

            private Rectangle sourceRect;

            bool crop;

            public bool Crop
            {
                get
                {
                    return crop;
                }
                set
                {
                    crop = value;
                    if (crop)
                    {
                        sourceRect.Width = Math.Min(sprite.SourceRect.Width, Rect.Width);
                        sourceRect.Height = Math.Min(sprite.SourceRect.Height, Rect.Height);
                    }
                }
            }

            public float Scale
            {
                get;
                set;
            }

            public Rectangle SourceRect
            {
                get { return sourceRect; }
                set { sourceRect = value; }
            }

            public GUIImage(Rectangle rect, string spritePath, Alignment alignment, GUIComponent parent = null)
                : this(rect, new Sprite(spritePath, Vector2.Zero), alignment, parent)
            {
            }

            public GUIImage(Rectangle rect, Sprite sprite, Alignment alignment, GUIComponent parent = null)
                : this(rect, sprite == null ? Rectangle.Empty : sprite.SourceRect, sprite, alignment, parent)
            {
            }

            public GUIImage(Rectangle rect, Rectangle sourceRect, Sprite sprite, Alignment alignment, GUIComponent parent = null)
                : base(null)
            {
                this.rect = rect;

                this.alignment = alignment;

                color = Color.White;

                //alpha = 1.0f;

                Scale = 1.0f;

                this.sprite = sprite;

                if (rect.Width == 0) this.rect.Width = (int)sprite.size.X;
                if (rect.Height == 0) this.rect.Height = (int)Math.Min(sprite.size.Y, sprite.size.Y * (this.rect.Width / sprite.size.X));

                this.sourceRect = sourceRect;

                if (parent != null) parent.AddChild(this);
                this.parent = parent;
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                if (!Visible) return;

                Color currColor = color;
                if (state == ComponentState.Hover) currColor = hoverColor;
                if (state == ComponentState.Selected) currColor = selectedColor;

                if (sprite != null && sprite.Texture != null)
                {
                    spriteBatch.Draw(sprite.Texture, new Vector2(rect.X, rect.Y), sourceRect, currColor * (currColor.A / 255.0f), Rotation, Vector2.Zero,
                        Scale, SpriteEffects.None, 0.0f);
                }

                DrawChildren(spriteBatch);
            }
        }
    }
}
