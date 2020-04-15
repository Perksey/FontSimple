using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;

namespace Ultz.FontSimple
{
    public static class TextImage
    {
        public static byte[] RenderBytes(RendererOptions font, string text, Rgba32 backColour, Rgba32 textColour, out Size size)
        {
            var img = RenderImage(font, text, backColour, textColour);
            var span = img.GetPixelSpan();
            var ret = new byte[span.Length * 4];
            var i = 0;
            foreach (var pixel in span)
            {
                ret[i++] = pixel.R;
                ret[i++] = pixel.G;
                ret[i++] = pixel.B;
                ret[i++] = pixel.A;
            }

            size = new Size(img.Width, img.Height);
            img.Dispose();
            return ret;
        }

        public static Image<Rgba32> RenderImage(RendererOptions font, string text, Rgba32 backColour, Rgba32 textColour)
        {
            var builder = new GlyphBuilder();
            var renderer = new TextRenderer(builder);
            var size = TextMeasurer.Measure(text, font);
            renderer.RenderText(text, font);

            builder.Paths
                .SaveImage((int) size.Width + 20, (int) size.Height + 20, out var ret, backColour, textColour);
            return ret;
        }

        public static void SaveImage(this IEnumerable<IPath> shapes, int width, int height, out Image<Rgba32> img,
            Rgba32 background, Rgba32 foreground)
        {
            img = new Image<Rgba32>(width, height);
            img.Mutate(x => x.Fill(background));

            foreach (IPath s in shapes)
            {
                // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                img.Mutate(x => x.Fill(foreground, s.Translate(Vector2.Zero)));
            }

            // img.Draw(Color.LawnGreen, 1, shape);
        }
    }
}