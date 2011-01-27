using System;
using osum.Graphics.Sprites;
using osum.Graphics.Drawables;
using osum.Helpers;
using OpenTK.Graphics;
using OpenTK;
#if IPHONE
using OpenTK.Graphics.ES11;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.OpenGLES;
using TextureTarget = OpenTK.Graphics.ES11.All;
using TextureParameterName = OpenTK.Graphics.ES11.All;
using EnableCap = OpenTK.Graphics.ES11.All;
using ArrayCap = OpenTK.Graphics.ES11.All;
using BlendingFactorSrc = OpenTK.Graphics.ES11.All;
using BlendingFactorDest = OpenTK.Graphics.ES11.All;
using PixelStoreParameter = OpenTK.Graphics.ES11.All;
using VertexPointerType = OpenTK.Graphics.ES11.All;
using ColorPointerType = OpenTK.Graphics.ES11.All;
using ClearBufferMask = OpenTK.Graphics.ES11.All;
using TexCoordPointerType = OpenTK.Graphics.ES11.All;
using BeginMode = OpenTK.Graphics.ES11.All;
using MatrixMode = OpenTK.Graphics.ES11.All;
using PixelInternalFormat = OpenTK.Graphics.ES11.All;
using PixelFormat = OpenTK.Graphics.ES11.All;
using PixelType = OpenTK.Graphics.ES11.All;
using ShaderType = OpenTK.Graphics.ES11.All;
using VertexAttribPointerType = OpenTK.Graphics.ES11.All;
using ProgramParameter = OpenTK.Graphics.ES11.All;
using ShaderParameter = OpenTK.Graphics.ES11.All;
using ErrorCode = OpenTK.Graphics.ES11.All;
using TextureEnvParameter = OpenTK.Graphics.ES11.All;
using TextureEnvTarget =  OpenTK.Graphics.ES11.All;
#else
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
#endif


namespace osum.Graphics.Drawables
{
	internal class ApproachCircle : pDrawable
	{
        internal float Radius;
        internal float Width = 6;

        public ApproachCircle(Vector2 position, float radius, bool alwaysDraw, float drawDepth, Color4 colour)
		{
            AlwaysDraw = alwaysDraw;
            DrawDepth = drawDepth;
            StartPosition = position;
            Position = position;
            Radius = radius;
            Colour = colour;
            Field = FieldTypes.Standard;
		}
		
		public override void Dispose()
		{
		}
		
		public override bool Draw()
		{
            if (base.Draw())
            {
                float scaleAdjustment = FieldScale.X / Scale.X;
				
				float rad1 = (Radius * ScaleScalar + Width * 0.5f) * scaleAdjustment;
                float rad2 = (Radius * ScaleScalar - Width * 0.5f) * scaleAdjustment;
                const int parts = 64;

                Vector2 pos = FieldPosition;
                Color4 c = AlphaAppliedColour;

                float[] vertices = new float[parts * 4 + 4];

                for (int v = 0; v < parts; v++)
                {
                    vertices[v * 4] = (float)(pos.X + Math.Cos(v * 2.0f * Math.PI / parts) * rad1);
                    vertices[v * 4 + 1] = (float)(pos.Y + Math.Sin(v * 2.0f * Math.PI / parts) * rad1);
                    vertices[v * 4 + 2] = (float)(pos.X + Math.Cos(v * 2.0f * Math.PI / parts) * rad2);
                    vertices[v * 4 + 3] = (float)(pos.Y + Math.Sin(v * 2.0f * Math.PI / parts) * rad2);
                }

                vertices[parts * 4] = vertices[0];
                vertices[parts * 4 + 1] = vertices[1];
                vertices[parts * 4 + 2] = vertices[2];
                vertices[parts * 4 + 3] = vertices[3];

                SpriteManager.TexturesEnabled = false;
				
				GL.Color4(c.R, c.G, c.B, c.A);
                GL.VertexPointer(2, VertexPointerType.Float, 0, vertices);
                GL.DrawArrays(BeginMode.TriangleStrip, 0, parts * 2 + 2);

                return true;
            }

            return false;

		}
	}
}

