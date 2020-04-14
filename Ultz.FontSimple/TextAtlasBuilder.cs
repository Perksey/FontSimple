using System;
using System.Collections.Generic;
using System.Drawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

namespace Ultz.FontSimple
{
    public class TextAtlasBuilder : IDisposable
    {
        private readonly ITextAtlas _atlas;
        private readonly RendererOptions _opts;
        private readonly int _expandY;
        private readonly Dictionary<char, Rectangle> _entries;
        private int _currentX;
        private int _currentY;
        
        public TextAtlasBuilder(ITextAtlas atlas, RendererOptions opts, int expandY)
        {
            _atlas = atlas;
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
            if (textSize.Width >= _atlas.Width)
            {
                throw new IndexOutOfRangeException("The atlas isn't wide enough to store this glyph.");
            }
            
            if (textSize.Height >= _atlas.Height)
            {
                throw new IndexOutOfRangeException("The atlas isn't tall enough to store this glyph.");
            }

            if (proposedX + textSize.Width >= _atlas.Width)
            {
                proposedX = 0;
                proposedY = _currentY + _opts.Font.LineHeight;
            }

            if (proposedY + textSize.Height >= _atlas.Height)
            {
                if (_atlas.CanResize)
                {
                    _atlas.Resize(_atlas.Width, Math.Max(_expandY, textSize.Height));
                    return GetPoint(textSize); // try again
                }

                throw new IndexOutOfRangeException(
                    "A glyph was not inserted into the atlas due to the atlas being full.");
            }

            _currentX = proposedX + textSize.Width;
            _currentY = proposedY + textSize.Height;
            return new Point(proposedX, proposedY);
        }

        public Rectangle AddCharacterToAtlas(char c)
        {
            if (!_atlas.CanWrite)
            {
                throw new InvalidOperationException("The text atlas is not configured to be written to.");
            }

            var rgbaBytes =
                TextImageBuilder.RenderBytes(_opts, c.ToString(), Rgba32.Transparent, Rgba32.White, out var size);
            var topLeft = GetPoint(size);
            _atlas.Insert(topLeft.X, topLeft.Y, size.Width, size.Height, rgbaBytes);
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
                    (float)pixels.X / _atlas.Width,
                    (float)pixels.Y / _atlas.Height,
                    (float)pixels.Width / _atlas.Width,
                    (float)pixels.Height / _atlas.Height
                );
            }

            return texCoords;
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
            _atlas?.Dispose();
        }
    }
}