﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpFont;
using System;
using System.Collections.Generic;

namespace Barotrauma
{
    public class ScalableFont
    {
        private static List<ScalableFont> FontList = new List<ScalableFont>();
        private static Library Lib = null;

        private string filename;
        private Face face;
        private uint size;
        private int baseHeight;
        //private int lineHeight;
        private Dictionary<uint, GlyphData> texCoords;
        private List<Texture2D> textures;
        private GraphicsDevice graphicsDevice;

        public uint Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                if (graphicsDevice!=null) RenderAtlas(graphicsDevice, charRanges, texDims, baseChar);
            }
        }

        private uint[] charRanges;
        private int texDims;
        private uint baseChar;

        private struct GlyphData
        {
            public int texIndex;
            public Vector2 drawOffset;
            public float advance;
            public Rectangle texCoords;
        }

        public ScalableFont(string filename, uint size, GraphicsDevice gd=null)
        {
            if (Lib == null) Lib = new Library();
            this.filename = filename;
            this.face = null;
            foreach (ScalableFont font in FontList)
            {
                if (font.filename == filename)
                {
                    this.face = font.face;
                    break;
                }
            }
            if (this.face == null)
            {
                this.face = new Face(Lib, filename);
            }
            this.size = size;

            this.textures = new List<Texture2D>();

            this.texCoords = new Dictionary<uint, GlyphData>();

            if (gd != null) RenderAtlas(gd);

            FontList.Add(this);
        }
        
        /// <summary>
        /// Renders the font into at least one texture atlas, which is simply a collection of all glyphs in the ranges defined by charRanges.
        /// Don't call this too often or with very large sizes.
        /// </summary>
        /// <param name="gd">Graphics device, required to create textures.</param>
        /// <param name="charRanges">Character ranges between each even element with their corresponding odd element. Default is 0x20 to 0xFFFF.</param>
        /// <param name="texDims">Texture dimensions. Default is 512x512.</param>
        /// <param name="baseChar">Base character used to shift all other characters downwards when rendering. Defaults to T.</param>
        public void RenderAtlas(GraphicsDevice gd, uint[] charRanges = null, int texDims = 512, uint baseChar = 0x54)
        {
            if (charRanges == null)
            {
                charRanges = new uint[] { 0x20, 0xFFFF };
            }
            this.charRanges = charRanges;
            this.texDims = texDims;
            this.baseChar = baseChar;

            face.SetPixelSizes(0, size);
            textures.ForEach(t => t.Dispose());
            textures.Clear();
            texCoords.Clear();

            uint[] pixelBuffer = new uint[texDims * texDims];
            for (int i = 0; i < texDims * texDims; i++)
            {
                pixelBuffer[i] = 0;
            }

            textures.Add(new Texture2D(gd, texDims, texDims, false, SurfaceFormat.Color));
            int texIndex = 0;

            Vector2 currentCoords = Vector2.Zero;
            int nextY = 0;

            face.LoadGlyph(face.GetCharIndex(baseChar), LoadFlags.Default, LoadTarget.Normal);
            baseHeight = face.Glyph.Metrics.Height.ToInt32();
            //lineHeight = baseHeight;
            for (int i = 0; i < charRanges.Length; i += 2)
            {
                uint start = charRanges[i];
                uint end = charRanges[i + 1];
                for (uint j = start; j <= end; j++)
                {
                    uint glyphIndex = face.GetCharIndex(j);
                    if (glyphIndex == 0) continue;
                    face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                    if (face.Glyph.Metrics.Width == 0 || face.Glyph.Metrics.Height == 0)
                    {
                        if (face.Glyph.Metrics.HorizontalAdvance > 0)
                        {
                            //glyph is empty, but char still applies advance
                            GlyphData blankData = new GlyphData();
                            blankData.advance = (float)face.Glyph.Metrics.HorizontalAdvance;
                            blankData.texIndex = -1; //indicates no texture because the glyph is empty
                            texCoords.Add(j, blankData);
                        }
                        continue;
                    }
                    //stacktrace doesn't really work that well when RenderGlyph throws an exception
                    face.Glyph.RenderGlyph(RenderMode.Normal);
                    byte[] bitmap = face.Glyph.Bitmap.BufferData;
                    int glyphWidth = face.Glyph.Bitmap.Width;
                    int glyphHeight = bitmap.Length / glyphWidth;

                    //if (glyphHeight>lineHeight) lineHeight=glyphHeight;

                    if (glyphWidth > texDims - 1 || glyphHeight > texDims - 1)
                    {
                        throw new Exception(filename + ", " + size.ToString() + ", "+ (char)j + "; Glyph dimensions exceed texture atlas dimensions");
                    }

                    nextY = Math.Max(nextY, glyphHeight + 2);

                    if (currentCoords.X + glyphWidth + 2 > texDims - 1)
                    {
                        currentCoords.X = 0;
                        currentCoords.Y += nextY;
                        nextY = 0;
                    }
                    if (currentCoords.Y + glyphHeight + 2 > texDims - 1)
                    {
                        currentCoords.X = 0;
                        currentCoords.Y = 0;
                        textures[texIndex].SetData<uint>(pixelBuffer);
                        textures.Add(new Texture2D(gd, texDims, texDims, false, SurfaceFormat.Color));
                        texIndex++;
                        for (int k = 0; k < texDims * texDims; k++)
                        {
                            pixelBuffer[k] = 0;
                        }
                    }

                    GlyphData newData = new GlyphData();
                    newData.advance = (float)face.Glyph.Metrics.HorizontalAdvance;
                    newData.texIndex = texIndex;
                    newData.texCoords = new Rectangle((int)currentCoords.X, (int)currentCoords.Y, glyphWidth, glyphHeight);
                    newData.drawOffset = new Vector2(face.Glyph.BitmapLeft, baseHeight*14/10 - face.Glyph.BitmapTop);
                    texCoords.Add(j, newData);

                    for (int y = 0; y < glyphHeight; y++)
                    {
                        for (int x = 0; x < glyphWidth; x++)
                        {
                            byte byteColor = bitmap[x + y * glyphWidth];
                            pixelBuffer[((int)currentCoords.X + x) + ((int)currentCoords.Y + y) * texDims] = (uint)(byteColor << 24 | byteColor << 16 | byteColor << 8 | byteColor);
                        }
                    }

                    currentCoords.X += glyphWidth + 2;
                }
                textures[texIndex].SetData<uint>(pixelBuffer);
            }

            graphicsDevice = gd;
        }
        
        public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects se, float layerDepth)
        {
            if (textures.Count == 0) return;
            
            int lineNum = 0;
            Vector2 currentPos = position;
            Vector2 advanceUnit = rotation == 0.0f ? Vector2.UnitX : new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lineNum++;
                    currentPos = position;
                    currentPos.X -= baseHeight * 1.8f * lineNum * advanceUnit.Y * scale.Y;
                    currentPos.Y += baseHeight * 1.8f * lineNum * advanceUnit.X * scale.Y;
                    continue;
                }
                uint charIndex = text[i];
                GlyphData gd;
                if (texCoords.TryGetValue(charIndex, out gd))
                {
                    if (gd.texIndex >= 0)
                    {
                        Texture2D tex = textures[gd.texIndex];
                        Vector2 drawOffset;
                        drawOffset.X = gd.drawOffset.X * advanceUnit.X * scale.X - gd.drawOffset.Y * advanceUnit.Y * scale.Y;
                        drawOffset.Y = gd.drawOffset.X * advanceUnit.Y * scale.Y + gd.drawOffset.Y * advanceUnit.X * scale.X;


                        sb.Draw(tex, currentPos + drawOffset, gd.texCoords, color, rotation, origin, scale, se, layerDepth);
                    }
                    currentPos += gd.advance * advanceUnit * scale.X;
                }
            }
        }

        public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects se, float layerDepth)
        {
            DrawString(sb, text, position, color, rotation, origin, new Vector2(scale), se, layerDepth);
        }

        public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color)
        {
            if (textures.Count == 0) return;

            Vector2 currentPos = position;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    currentPos.X = position.X;
                    currentPos.Y += baseHeight * 1.8f;
                    continue;
                }
                uint charIndex = text[i];
                GlyphData gd;
                if (texCoords.TryGetValue(charIndex, out gd))
                {
                    if (gd.texIndex >= 0)
                    {
                        Texture2D tex = textures[gd.texIndex];
                        sb.Draw(tex, currentPos + gd.drawOffset, gd.texCoords, color);
                    }
                    currentPos.X += gd.advance;
                }
            }
        }
    
        public Vector2 MeasureString(string text)
        {
            float currentLineX = 0.0f;
            Vector2 retVal = Vector2.Zero;
            retVal.Y = baseHeight*1.8f;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    currentLineX = 0.0f;
                    retVal.Y += baseHeight*18/10;
                    continue;
                }
                uint charIndex = text[i];
                GlyphData gd;
                if (texCoords.TryGetValue(charIndex, out gd))
                {
                    currentLineX += gd.advance;
                }
                retVal.X = Math.Max(retVal.X,currentLineX);
            }
            return retVal;
        }
    }
}
