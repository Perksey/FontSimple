using System;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;

namespace Ultz.FontSimple.OpenGL
{
    public class GlTextAtlas : ITextAtlas
    {
        private GL _gl;

        private ArbCopyImage _arbCopyImage;

        // TODO ArbDirectStateAccess _arbDirectStateAccess, bool _isVersion45OrLater
        public uint TextureHandle { get; private set; }
        public event Action<uint, uint> TextureRecreated; // Old, New 
        private bool _allowResize;
        private bool _isVersion43OrLater;
        private int _width, _height;

        public unsafe GlTextAtlas(GL gl, int width, int height, bool allowResize)
        {
            _gl = gl;
            _gl.TryGetExtension(out _arbCopyImage);
            _gl.GetInteger(GLEnum.MajorVersion, out var major);
            _gl.GetInteger(GLEnum.MinorVersion, out var minor);
            if (major > 4 || major == 4 && minor >= 3)
            {
                _isVersion43OrLater = true;
            }

            TextureHandle = _gl.GenTexture();
            _gl.BindTexture(GLEnum.Texture2D, TextureHandle);
            _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba, (uint) width, (uint) height, 0, GLEnum.Rgba,
                GLEnum.UnsignedByte, null);
            _gl.BindTexture(GLEnum.Texture2D, 0);
            _allowResize = allowResize;
            _width = width;
            _height = height;
        }

        public GlTextAtlas(GL gl, uint texture, bool allowResize)
        {
            _gl = gl;
            _gl.TryGetExtension(out _arbCopyImage);
            TextureHandle = texture;
            _allowResize = allowResize;
            _gl.GetTexLevelParameter(GLEnum.Texture2D, 0, GLEnum.TextureWidth, out _width);
            _gl.GetTexLevelParameter(GLEnum.Texture2D, 0, GLEnum.TextureHeight, out _height);
        }

        public int Width => _width;
        public int Height => _height;
        public bool CanResize => _allowResize;
        public bool CanRead { get; } = true;
        public bool CanWrite { get; } = true;

        public void Insert(int x, int y, int width, int height, ReadOnlySpan<byte> data)
        {
            _gl.BindTexture(GLEnum.Texture2D, TextureHandle);
            var pr = data.GetPinnableReference();
            _gl.TexSubImage2D(GLEnum.Texture2D, 0, x, y, (uint) width, (uint) height, GLEnum.Rgba, GLEnum.UnsignedByte,
                ref pr);
            _gl.BindTexture(GLEnum.Texture2D, 0);
        }

        public unsafe ReadOnlySpan<byte> GetRegion(int x, int y, int width, int height)
        {
            Span<byte> ret = new byte[width * height * 4];
            fixed (void* pixels = ret)
            {
                _gl.GetTexImage(GLEnum.Texture2D, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
            }

            return ret;
        }

        public unsafe void Resize(int width, int height)
        {
            if (!_allowResize)
            {
                throw new NotSupportedException("This atlas is not configured to be resized.");
            }

            // Create the new texture
            var dstTexId = _gl.GenTexture();
            _gl.BindTexture(GLEnum.Texture2D, dstTexId);
            _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba, (uint) width, (uint) height, 0, GLEnum.Rgba,
                GLEnum.UnsignedByte, null);
            _gl.BindTexture(GLEnum.Texture2D, 0);

            if (!(_arbCopyImage is null))
            {
                _arbCopyImage.CopyImageSubData(TextureHandle, ARB.Texture2D, 0, 0, 0, 0, dstTexId, ARB.Texture2D, 0, 0,
                    0, 0, (uint) _width, (uint) _height, 1);
            }
            else if (_isVersion43OrLater)
            {
                _gl.CopyImageSubData(TextureHandle, GLEnum.Texture2D, 0, 0, 0, 0, dstTexId, GLEnum.Texture2D, 0, 0,
                    0, 0, (uint) _width, (uint) _height, 1);
            }
            else
            {
                // Create a framebuffer, so that we can copy the old texture to the new texture via the framebuffer
                var fboId = _gl.GenFramebuffer();
                _gl.BindFramebuffer(GLEnum.Framebuffer, fboId);
                _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, TextureHandle,
                    0);
                _gl.BindTexture(GLEnum.Texture2D, dstTexId);
                _gl.CopyTexImage2D(GLEnum.Texture2D, 0, GLEnum.Rgba, 0, 0, (uint) width, (uint) height, 0);

                // Unbind our resources
                _gl.BindTexture(GLEnum.Texture2D, 0);
                _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
                _gl.DeleteFramebuffer(fboId);
            }

            // Let the user know
            TextureRecreated?.Invoke(TextureHandle, dstTexId);

            // Delete the old texture
            _gl.DeleteTexture(TextureHandle);

            // Update our variables
            TextureHandle = dstTexId;
            _width = width;
            _height = height;
        }

        private void ReleaseUnmanagedResources()
        {
            _gl?.DeleteTexture(TextureHandle);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            _arbCopyImage?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~GlTextAtlas()
        {
            ReleaseUnmanagedResources();
        }
    }
}