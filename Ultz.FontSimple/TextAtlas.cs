using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using Ultz.FontSimple.Graphics;

namespace Ultz.FontSimple
{
    public class TextAtlas : IDisposable
    {
        public ITextAtlas Atlas { get; }
        private readonly RendererOptions _opts;
        private readonly int _expandY;
        private readonly Dictionary<char, Rectangle> _entries;
        private int _currentX;
        private int _currentY;
        
        public TextAtlas(ITextAtlas atlas, RendererOptions opts, int expandY)
        {
            Atlas = atlas;
            _opts = opts;
            _expandY = expandY;
            _entries = new Dictionary<char, Rectangle>();
            _currentX = 0;
            _currentY = 0;
        }

        public Point GetPoint(Size textSize)
        {
            var proposedX = _currentX;
            var proposedY = _currentY;
            if (textSize.Width >= Atlas.Width)
            {
                throw new IndexOutOfRangeException("The atlas isn't wide enough to store this glyph.");
            }
            
            if (textSize.Height >= Atlas.Height)
            {
                throw new IndexOutOfRangeException("The atlas isn't tall enough to store this glyph.");
            }

            if (proposedX + textSize.Width >= Atlas.Width)
            {
                proposedX = 0;
                proposedY = _currentY + _opts.Font.LineHeight;
            }

            if (proposedY + textSize.Height >= Atlas.Height)
            {
                if (Atlas.CanResize)
                {
                    Atlas.Resize(Atlas.Width, Math.Max(_expandY, textSize.Height));
                    return GetPoint(textSize); // try again
                }

                throw new IndexOutOfRangeException(
                    "A glyph was not inserted into the atlas due to the atlas being full.");
            }

            _currentX = proposedX + textSize.Width;
            _currentY = proposedY;
            return new Point(proposedX, proposedY);
        }

        public Rectangle AddCharacterToAtlas(char c)
        {
            if (!Atlas.CanWrite)
            {
                throw new InvalidOperationException("The text atlas is not configured to be written to.");
            }

            if (c == '\n')
            {
                throw new InvalidOperationException("Can't add new-lines to the atlas," +
                                                    "these need to be handled manually.");
            }

            var rgbaBytes =
                TextImage.RenderBytes(_opts, c.ToString(), Rgba32.Transparent, Rgba32.White, out var size);
            var topLeft = GetPoint(size);
            Atlas.Insert(topLeft.X, topLeft.Y, size.Width, size.Height, rgbaBytes);
            var rect = new Rectangle(topLeft, size);
            _entries.Add(c, rect);
            return rect;
        }

        public ReadOnlySpan<RectangleF> GetTexCoords(ReadOnlySpan<char> characters)
        {
            var pixelCoords = GetPixelCoords(characters);
            var texCoords = new RectangleF[pixelCoords.Length];
            for (var i = 0; i < pixelCoords.Length; i++)
            {
                var pixels = pixelCoords[i];
                texCoords[i] = new RectangleF
                (
                    (float)pixels.X / Atlas.Width,
                    (float)pixels.Y / Atlas.Height,
                    (float)pixels.Width / Atlas.Width,
                    (float)pixels.Height / Atlas.Height
                );
            }

            return texCoords;
        }

        public ReadOnlySpan<Quad> GetPixelQuads(Rectangle pixelRegion, ReadOnlySpan<char> text)
        {
            var currentX = pixelRegion.X;
            var currentY = pixelRegion.Y;
            var quads = new Quad[text.Length];
            Span<char> sittingArea = stackalloc char[1]; // where chars "sit" before being sent off to GetTexCoords
            var j = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\n')
                {
                    currentX = pixelRegion.X;
                    currentY += _opts.Font.LineHeight;
                    if (currentY > pixelRegion.Bottom)
                    {
                        throw new ArgumentException("The given region isn't tall enough to contain the requested text",
                            nameof(pixelRegion));
                    }
                }
                else
                {
                    sittingArea[0] = c;
                    var pixelCoords = GetPixelCoords(sittingArea)[0];
                    if (currentX + pixelCoords.Width > pixelRegion.Right)
                    {
                        throw new ArgumentException("The given region isn't wide enough to contain the requested text",
                            nameof(pixelRegion));
                    }
                    
                    if (currentY + pixelCoords.Height > pixelRegion.Bottom)
                    {
                        throw new ArgumentException("The given region isn't tall enough to contain the requested text",
                            nameof(pixelRegion));
                    }
                    
                    quads[j++] = new Quad(new Point(currentX, currentY), pixelCoords.Size, pixelCoords);
                    currentX += pixelCoords.Width;
                }
            }

            return ((Span<Quad>)quads).Slice(0, j);
        }

        public ReadOnlySpan<Quad> GetNormalizedQuads(RectangleF region, Size screenSize, ReadOnlySpan<char> text)
        {
            var pixelRegion = new Rectangle
            (
                (int)(screenSize.Width * 0.5 * region.X + screenSize.Width * 0.5),
                (int)(screenSize.Height * 0.5 * region.Y + screenSize.Height * 0.5),
                (int)(screenSize.Width * 0.5 * region.Right + screenSize.Width * 0.5),
                (int)(screenSize.Height * 0.5 * region.Y - region.Height + screenSize.Height * 0.5)
            );

            var quads = GetPixelQuads(pixelRegion, text);
            var ret = new Quad[quads.Length];
            var ortho = Matrix4x4.CreateOrthographicOffCenter(0, screenSize.Width, 0, screenSize.Height, 1, -1);
            var textureSize = new Size(Atlas.Width, Atlas.Height);
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = new Quad
                (
                    quads[i],
                    ortho,
                    textureSize
                );
            }

            return ret;
        }

        public ReadOnlySpan<Rectangle> GetPixelCoords(ReadOnlySpan<char> characters)
        {
            var ret = new Rectangle[characters.Length];
            for (var i = 0; i < characters.Length; i++)
            {
                var c = characters[i];
                ret[i] = _entries.ContainsKey(c) ? _entries[c] : AddCharacterToAtlas(c);
            }

            return ret;
        }

        public void Dispose()
        {
            Atlas?.Dispose();
        }
    }
}