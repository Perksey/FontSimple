using System;
using Veldrid;

#nullable enable

namespace Ultz.FontSimple.Veldrid
{
    public class VeldridTextAtlas : ITextAtlas
    {
        public Texture Texture { get; private set; }
        public event Action<Texture, Texture> TextureRecreated; // Old, New
        private GraphicsDevice? _graphicsDevice;
        private bool _allowResize;
        private CommandList _commandList;
        private object _lock;

        public VeldridTextAtlas(int width, int height, GraphicsDevice device, bool allowResize)
            : this(device.ResourceFactory.CreateTexture(new TextureDescription((uint) width, (uint) height, 1, 1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D)), device, allowResize)
        {
        }

        public VeldridTextAtlas(Texture texture, GraphicsDevice device, bool allowResize)
        {
            if (texture.Format != PixelFormat.R8_G8_B8_A8_UNorm)
            {
                throw new InvalidOperationException($"{texture.Format} is not supported." +
                                                    "Currently, only R8_G8_B8_A8_UNorm is supported");
            }

            Texture = texture;
            _lock = new object();
            _graphicsDevice = device;
            _allowResize = allowResize;
            _commandList = device.ResourceFactory.CreateCommandList();
        }

        public int Width => (int) Texture.Width;
        public int Height => (int) Texture.Height;
        public bool CanResize => _allowResize && !(_graphicsDevice is null);
        public bool CanRead { get; } = false;
        public bool CanWrite { get; } = true;

        public unsafe void Insert(int x, int y, int width, int height, ReadOnlySpan<byte> data)
        {
            fixed (byte* buffer = data)
            {
                _graphicsDevice.UpdateTexture(Texture, (IntPtr) buffer, (uint) data.Length, (uint) x, (uint) y, 0,
                    (uint) width, (uint) height, 1, 0, 0);
            }
        }

        public ReadOnlySpan<byte> GetRegion(int x, int y, int width, int height)
        {
            throw new NotSupportedException("This atlas does not support being read from.");
        }

        public void Resize(int width, int height)
        {
            if (!_allowResize || _graphicsDevice is null)
            {
                throw new NotSupportedException("This atlas is not configured to be resized.");
            }

            var newTexture = _graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription((uint) width,
                (uint) height, 1, 1, 1, Texture.Format, Texture.Usage, Texture.Type, Texture.SampleCount));
            lock (_lock)
            {
                _commandList.Begin();
                _commandList.CopyTexture(Texture, newTexture);
                _commandList.End();
                _graphicsDevice.SubmitCommands(_commandList);
            }
            
            TextureRecreated?.Invoke(Texture, newTexture);
            Texture = newTexture;
        }

        public void Dispose()
        {
            Texture.Dispose();
            _commandList.Dispose();
        }
    }
}