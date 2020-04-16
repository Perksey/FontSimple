using System.Drawing;
using System.Numerics;

namespace Ultz.FontSimple.Graphics
{
    public struct Quad
    {
        public Quad(Point location, Size size, Rectangle texCoords)
        {
            TopLeft = new TexturedVertex2D
            {
                Position = new Vector2(location.X, location.Y),
                TexCoords = new Vector2(texCoords.Left, texCoords.Top)
            };
            TopRight = new TexturedVertex2D
            {
                Position = new Vector2(location.X + size.Width, location.Y),
                TexCoords = new Vector2(texCoords.Right, texCoords.Top)
            };
            BottomRight = new TexturedVertex2D
            {
                Position = new Vector2(location.X + size.Width, location.Y + size.Height),
                TexCoords = new Vector2(texCoords.Right, texCoords.Bottom)
            };
            BottomLeft = new TexturedVertex2D
            {
                Position = new Vector2(location.X, location.Y + size.Height),
                TexCoords = new Vector2(texCoords.Left, texCoords.Bottom)
            };
        }

        internal Quad(Quad original, Matrix4x4 projection, Size textureDimensions)
        {
            var positionMatrix = new Matrix4x4
            (
                original.TopLeft.Position.X, original.TopLeft.Position.Y, 0, 0,
                original.TopRight.Position.X, original.TopRight.Position.Y, 0, 0,
                original.BottomRight.Position.X, original.BottomRight.Position.Y, 0, 0,
                original.BottomLeft.Position.X, original.BottomLeft.Position.Y, 0, 0
            );

            var newMatrix = positionMatrix * projection;
            TopLeft = new TexturedVertex2D
            {
                Position = new Vector2(newMatrix.M11 - 1, newMatrix.M12 - 1),
                TexCoords = new Vector2(original.TopLeft.TexCoords.X / textureDimensions.Width,
                    original.TopLeft.TexCoords.Y / textureDimensions.Height)
            };
            TopRight = new TexturedVertex2D
            {
                Position = new Vector2(newMatrix.M21 - 1, newMatrix.M22 - 1),
                TexCoords = new Vector2(original.TopRight.TexCoords.X / textureDimensions.Width,
                    original.TopRight.TexCoords.Y / textureDimensions.Height)
            };
            BottomRight = new TexturedVertex2D
            {
                Position = new Vector2(newMatrix.M31 - 1, newMatrix.M32 - 1),
                TexCoords = new Vector2(original.BottomRight.TexCoords.X / textureDimensions.Width,
                    original.BottomRight.TexCoords.Y / textureDimensions.Height)
            };
            BottomLeft = new TexturedVertex2D
            {
                Position = new Vector2(newMatrix.M41 - 1, newMatrix.M42 - 1),
                TexCoords = new Vector2(original.BottomLeft.TexCoords.X / textureDimensions.Width,
                    original.BottomLeft.TexCoords.Y / textureDimensions.Height)
            };
        }

        public TexturedVertex2D TopLeft { get; set; }
        public TexturedVertex2D TopRight { get; set; }
        public TexturedVertex2D BottomRight { get; set; }
        public TexturedVertex2D BottomLeft { get; set; }

        public readonly QuadIndex[] GetTriangleListIndices() => new[]
        {
            QuadIndex.TopLeft, QuadIndex.TopRight, QuadIndex.BottomRight, QuadIndex.TopLeft, QuadIndex.BottomRight,
            QuadIndex.BottomLeft
        };

        public readonly QuadIndex[] GetTriangleStripIndices() => new[]
            {QuadIndex.TopLeft, QuadIndex.TopRight, QuadIndex.BottomRight, QuadIndex.BottomLeft};
    }
}