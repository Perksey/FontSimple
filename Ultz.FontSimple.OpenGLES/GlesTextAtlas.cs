using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace Ultz.FontSimple.OpenGLES
{
    public class GlesTextAtlas : ITextAtlas
    {
        private GL _gl;
        public uint TextureHandle { get; private set; }
        public event Action<uint, uint> TextureRecreated; // Old, New 
        private bool _allowResize;
        private int _width, _height;
        public unsafe GlesTextAtlas(GL gl, int width, int height, bool allowResize)
        {
            _gl = gl;
            TextureHandle = _gl.GenTexture();
            _gl.BindTexture(GLEnum.Texture2D, TextureHandle);
            _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba, (uint) width, (uint) height, 0, GLEnum.Rgba,
                GLEnum.UnsignedByte, null);
            _gl.BindTexture(GLEnum.Texture2D, 0);
            _allowResize = allowResize;
            _width = width;
            _height = height;
        }
        
        public GlesTextAtlas(GL gl, uint texture, bool allowResize)
        {
            _gl = gl;
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
            // Create a framebuffer, so that we can retrieve the pixels using glReadPixels
            var fboId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(GLEnum.Framebuffer, fboId);
            _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, TextureHandle, 0);
            Span<byte> ret = new byte[width * height * 4];
            fixed (void* pixels = ret)
            {
                _gl.ReadPixels(x, y, (uint) width, (uint) height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
            }
            
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            _gl.DeleteFramebuffer(fboId);
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
            
            // Create a framebuffer, so that we can copy the old texture to the new texture via the framebuffer
            var fboId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(GLEnum.Framebuffer, fboId);
            _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, TextureHandle, 0);
            _gl.BindTexture(GLEnum.Texture2D, dstTexId);
            _gl.CopyTexImage2D(GLEnum.Texture2D, 0, GLEnum.Rgba, 0, 0, (uint)width, (uint)height, 0);

            // Unbind our resources
            _gl.BindTexture(GLEnum.Texture2D, 0);
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            
            // Let the user know
            TextureRecreated?.Invoke(TextureHandle, dstTexId);
            
            // Delete the old resources
            _gl.DeleteTexture(TextureHandle);
            _gl.DeleteFramebuffer(fboId);
            
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
            GC.SuppressFinalize(this);
        }

        ~GlesTextAtlas()
        {
            ReleaseUnmanagedResources();
        }
    }
}