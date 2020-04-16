namespace Ultz.FontSimple.Graphics
{
    public static class FontSimpleGlsl
    {
        public const string DefaultVertexCode = @"
#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 0) out vec2 fsin_TexCoords;
void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_TexCoords = TexCoords;
}";

        public const string DefaultFragmentCode = @"
#version 450
layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 fsout_Color;
layout(set = 0, binding = 0) uniform texture2D Texture;
layout(set = 0, binding = 1) uniform sampler Sampler;
void main()
{
    fsout_Color =  texture(sampler2D(Texture, Sampler), fsin_TexCoords);
}";
    }
}