using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Ultz.FontSimple;

namespace Prototyping
{
    class Program
    {
        static void Main(string[] args)
        {
            using var bytes = TextImageBuilder.RenderImage(
                new RendererOptions(SystemFonts.Collection.CreateFont("Arial", 12f)), "Hello, world!",
                Rgba32.CornflowerBlue, Rgba32.White);
            using var output = File.OpenWrite("File.png");
            bytes.SaveAsPng(output);
            output.Flush();
        }
    }
}