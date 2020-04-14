using System;

namespace Ultz.FontSimple
{
    public interface ITextAtlas : IDisposable
    {
        int Width { get; }
        int Height { get; }
        bool CanResize { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        void Insert(int x, int y, int width, int height, ReadOnlySpan<byte> data);
        ReadOnlySpan<byte> GetRegion(int x, int y, int width, int height);
        void Resize(int width, int height);
    }
}