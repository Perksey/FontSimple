using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using SixLabors.Fonts;
using Ultz.FontSimple;
using Ultz.FontSimple.Graphics;
using Ultz.FontSimple.Veldrid;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace VeldridExample
{
    class Program : IDisposable
    {
        private GraphicsDevice _graphicsDevice;
        private Sdl2Window _window;
        private CommandList _commandList;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private Shader[] _shaders;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _resourceLayout;
        private TextAtlas _textAtlas;
        private Texture _texture;
        private TextureView _textureView;
        private bool _initialized;
        private bool _needsUpdate = true;
        private Size _windowSize;

        static void Main()
        {
            using var instance = new Program();
            instance.Run();
        }

        private void Run()
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 500, 500, WindowState.Normal, "FontSimple Veldrid Example"),
                out _window, out _graphicsDevice);
            _window.Resized += () => { _needsUpdate = true; };
            while (_window.Exists)
            {
                _window.PumpEvents();
                if (_window.Exists)
                {
                    if (!_initialized)
                    {
                        Init();
                    }

                    if (_needsUpdate)
                    {
                        Update();
                    }

                    DrawFrame();
                }
            }
        }

        private void Init()
        {
            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
            _resourceLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly,
                        ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _shaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(FontSimpleGlsl.DefaultVertexCode),
                    "main", Debugger.IsAttached),
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FontSimpleGlsl.DefaultFragmentCode),
                    "main", Debugger.IsAttached));
            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend, DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.CullNone, PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementFormat.Float2,
                                VertexElementSemantic.TextureCoordinate),
                            new VertexElementDescription("TexCoords", VertexElementFormat.Float2,
                                VertexElementSemantic.TextureCoordinate))
                    }, _shaders), _resourceLayout, _graphicsDevice.SwapchainFramebuffer.OutputDescription));
            _textAtlas = new TextAtlas(new VeldridTextAtlas(1024, 1024, _graphicsDevice, false),
                new RendererOptions(SystemFonts.CreateFont("Arial", 24)), 256);
            _texture = ((VeldridTextAtlas) _textAtlas.Atlas).Texture;
            ((VeldridTextAtlas) _textAtlas.Atlas).TextureRecreated += (old, @new) =>
            {
                _texture = @new;
                _textureView?.Dispose();
                _textureView = null;
                _needsUpdate = true;
            };
            _initialized = true;
        }

        private unsafe void Update()
        {
            _windowSize = new Size(_window.Width, _window.Height);
            
            // Get the quads from which the vertices are sourced
            var quads = _textAtlas.GetNormalizedQuads(new RectangleF(-1, 0.2f, 2, 0.4f), _windowSize, "Hello, world!");
            
            // Create/update the vertex buffer
            var len = quads.Length * sizeof(Quad);
            if (_vertexBuffer is null || _vertexBuffer.SizeInBytes != len)
            {
                _vertexBuffer?.Dispose();
                _vertexBuffer =
                    _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) len,
                        BufferUsage.VertexBuffer));
            }

            fixed (Quad* ptr = quads)
            {
                _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, (IntPtr) ptr, (uint) len);
            }

            // Create/update the index buffer
            var indices = stackalloc uint[quads.Length * 6];
            for (var i = 0; i < quads.Length; i++)
            {
                var currentIndexIndex = i * 6;
                var currentVertexIndex = i * 4;
                var quadIndices = stackalloc QuadIndex[]
                {
                    // Copied from Quad.cs - I didn't want to allocate an array for each quad so we'll stackalloc here.
                    QuadIndex.TopLeft, QuadIndex.TopRight, QuadIndex.BottomRight,
                    QuadIndex.TopLeft, QuadIndex.BottomRight, QuadIndex.BottomLeft
                };
                for (var j = 0; j < 6; j++)
                {
                    indices[currentIndexIndex + j] = (uint) (currentVertexIndex + (uint) quadIndices[j]);
                }
            }

            len = quads.Length * 6 * sizeof(uint);
            if (_indexBuffer is null || _indexBuffer.SizeInBytes != len)
            {
                _indexBuffer?.Dispose();
                _indexBuffer =
                    _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) len,
                        BufferUsage.IndexBuffer));
            }

            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, (IntPtr) indices, (uint) len);
            
            // Create the resource set
            if (_textureView is null)
            {
                _textureView = _graphicsDevice.ResourceFactory.CreateTextureView(_texture);
                _resourceSet?.Dispose();
                _resourceSet =
                    _graphicsDevice.ResourceFactory.CreateResourceSet(
                        new ResourceSetDescription(_resourceLayout, _textureView, _graphicsDevice.Aniso4xSampler));
            }
        }

        private void DrawFrame()
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _resourceSet);
            _commandList.DrawIndexed(_indexBuffer.SizeInBytes / sizeof(uint));
            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }

        public void Dispose()
        {
            _texture?.Dispose();
            _textureView?.Dispose();
            _resourceSet?.Dispose();
            _resourceLayout?.Dispose();
            _pipeline?.Dispose();
            _commandList?.Dispose();
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _textAtlas?.Dispose();
            _graphicsDevice?.Dispose();
        }
    }
}