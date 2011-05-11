using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using osum.Helpers;
using OpenTK;
using osu_common.Helpers;
using osum.GameplayElements;
#if iOS
using OpenTK.Graphics.ES11;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.OpenGLES;

using TextureTarget = OpenTK.Graphics.ES11.All;
using TextureParameterName = OpenTK.Graphics.ES11.All;
using EnableCap = OpenTK.Graphics.ES11.All;
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
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
#else
using OpenTK.Graphics.OpenGL;
using osum.Input;
#endif

namespace osum.Graphics.Sprites
{
    internal partial class pDrawable : IDrawable, IDisposable, IComparable<pDrawable>
    {
        internal float Alpha;

        internal bool AlwaysDraw;
        internal ClockTypes Clocking;

        internal Color4 Colour;
        /*protected Color4 colour;
        internal Color4 Colour
        {
            get { return colour; }
            set { colour = value; StartColour = value; }
        }
        
        internal Color4 StartColour;*/

        internal bool Disposable;
        internal float Rotation;
        internal Vector2 Scale = Vector2.One;
        internal Vector2 StartPosition;
        internal Vector2 Offset;
        public object Tag;
        public int TagNumeric;
        internal FieldTypes Field = FieldTypes.Standard;
        internal OriginTypes Origin;
        internal virtual Vector2 OriginVector { get { return Vector2.Zero; } }
        internal Vector2 Position;
        internal BlendingFactorDest BlendingMode = BlendingFactorDest.OneMinusSrcAlpha;

        internal virtual bool IsOnScreen
        {
            get
            {
                Vector2 pos = FieldPosition;

                if (pos.X > GameBase.NativeSize.Width || pos.X < 0 ||
                    pos.Y > GameBase.NativeSize.Height || pos.Y < 0)
                    return false;

                return true;
            }
        }

        internal float DrawDepth;

        /// <summary>
        /// Determines whether the sprite automatically remove past transformations.
        /// </summary>
        internal bool RemoveOldTransformations = true;

        /// <summary>
        /// Important: don't use this to add new transformations, use pSprite.Transform() for that.
        /// </summary>
        public pList<Transformation> Transformations = new pList<Transformation>() { UseBackwardsSearch = true };

        internal virtual bool IsRemovable
        {
            get { return !AlwaysDraw && Transformations.Count == 0; }
        }

        internal virtual bool UsesTextures
        {
            get { return false; }
        }

        internal float ScaleScalar
        {
            get { return Scale.X; }
            set { Scale = new Vector2(value, value); }
        }

        internal bool Additive
        {
            get { return BlendingMode == BlendingFactorDest.One; }
            set { BlendingMode = value ? BlendingFactorDest.One : BlendingFactorDest.OneMinusSrcAlpha; }
        }

        /// <summary>
        /// Gets the display rectangle (base size).
        /// </summary>
        protected virtual Box2 DisplayRectangle
        {
            get
            {
                Vector2 pos = FieldPosition / GameBase.BaseToNativeRatio;
                Vector2 scale = FieldScale / GameBase.BaseToNativeRatio;
                Vector2 origin = OriginVector;
                return new Box2(pos.X - origin.X, pos.Y - origin.Y, pos.X + scale.X, pos.Y + scale.Y);
            }
        }

        internal virtual Vector2 FieldPosition
        {
            get
            {
                Vector2 fieldPosition;

                Vector2 pos = Position;

                if (Offset != Vector2.Zero)
                    pos += Offset;

                pos *= GameBase.BaseToNativeRatio;

                if (AlignToSprites)
                    pos *= 960f / GameBase.SpriteResolution;

                switch (Field)
                {
                    case FieldTypes.StandardSnapCentre:
                        fieldPosition = new Vector2(GameBase.NativeSize.Width / 2 + pos.X,
                                                    GameBase.NativeSize.Height / 2 + pos.Y);
                        break;
                    case FieldTypes.StandardSnapBottomCentre:
                        fieldPosition = new Vector2(GameBase.NativeSize.Width / 2 + pos.X,
                                                    GameBase.NativeSize.Height - pos.Y);
                        break;
                    case FieldTypes.StandardSnapTopCentre:
                        fieldPosition = new Vector2(GameBase.NativeSize.Width / 2 + pos.X,
                                                    pos.Y);
                        break;
                    case FieldTypes.StandardSnapRight:
                        fieldPosition = new Vector2(GameBase.NativeSize.Width - pos.X, pos.Y);
                        break;
                    case FieldTypes.StandardSnapBottomLeft:
                        fieldPosition = new Vector2(pos.X, GameBase.NativeSize.Height - pos.Y);
                        break;
                    case FieldTypes.StandardSnapBottomRight:
                        fieldPosition = new Vector2(GameBase.NativeSize.Width - pos.X,
                                                    GameBase.NativeSize.Height - pos.Y);
                        break;
                    case FieldTypes.GamefieldStandardScale:
                    case FieldTypes.GamefieldSprites:
                    case FieldTypes.GamefieldExact:
                        fieldPosition = Position;
                        GameBase.GamefieldToStandard(ref fieldPosition);
                        Vector2.Multiply(ref fieldPosition, GameBase.BaseToNativeRatio, out fieldPosition);
                        break;
                    case FieldTypes.NativeScaled:
                        return Position;
                    case FieldTypes.Native:
                    default:
                        return pos;
                }

                return fieldPosition;
            }
        }

        /// <summary>
        /// Because the resolution of sprites is not 1:1 to the resizing of the window (ie. between 960-1024 widths, where it stays constant)
        /// an extra ratio calculation must be applied to keep sprites aligned.
        /// </summary>
        internal bool AlignToSprites;

        internal virtual Vector2 FieldScale
        {
            get
            {
                switch (Field)
                {
                    case FieldTypes.GamefieldExact:
                        return Scale * DifficultyManager.HitObjectRadius;
                    case FieldTypes.GamefieldSprites:
                        return Scale * (DifficultyManager.HitObjectSizeModifier * GameBase.SpriteToNativeRatio);
                    case FieldTypes.Native:
                    case FieldTypes.NativeScaled:
                        return Scale;
                    default:
                        if (UsesTextures)
                            return Scale * GameBase.SpriteToNativeRatio;
                        if (AlignToSprites)
                            return Scale * 960f / GameBase.SpriteResolution * GameBase.BaseToNativeRatio;
                        else
                            return Scale * GameBase.BaseToNativeRatio;
                }
            }
        }

        /// <summary>
        /// If true, this sprite is not affected by universal dimming.
        /// </summary>
        internal bool DimImmune;

        internal Color4 AlphaAppliedColour
        {
            get
            {
                if (SpriteManager.UniversalDim > 0 && !DimImmune)
                {
                    float dim = 1 - SpriteManager.UniversalDim;
                    return new Color4(Colour.R * dim, Colour.G * dim, Colour.B * dim, Alpha * Colour.A);
                }

                return Alpha < 1 ? new Color4(Colour.R, Colour.G, Colour.B, Alpha * Colour.A) : Colour;
            }
        }

        internal void Transform(Transformation transform)
        {
            transform.Clocking = Clocking;
            Transformations.AddInPlace(transform);
        }

        internal void Transform(params Transformation[] transforms)
        {
            foreach (Transformation t in transforms)
                Transform(t);
        }

        internal void Transform(IEnumerable<Transformation> transforms)
        {
            foreach (Transformation t in transforms)
                Transform(t);
        }

        /// <summary>
        /// Iterates through each tansformation and applies where necessary.
        /// </summary>
        private void UpdateTransformations()
        {
            bool hasColour = false;
            bool hasAlpha = false;
            bool hasRotation = false;
            bool hasScale = false;
            bool hasMovement = false;
            bool hasMovementX = false;

            for (int i = 0; i < Transformations.Count; i++)
            {
                Transformation t = Transformations[i];

                // remove old transformations
                if (t.Terminated)
                {
                    switch (t.Type)
                    {
                        case TransformationType.Colour:
                            Colour = t.EndColour;
                            if (!RemoveOldTransformations)
                                hasColour = true;
                            break;

                        case TransformationType.Fade:
                            Alpha = t.EndFloat;
                            if (!RemoveOldTransformations)
                                hasAlpha = true;
                            break;

                        case TransformationType.Movement:
                            Position = t.EndVector;
                            if (!RemoveOldTransformations)
                                hasMovement = true;
                            break;

                        case TransformationType.MovementX:
                            Position.X = t.EndFloat;
                            if (!RemoveOldTransformations)
                                hasMovementX = true;
                            break;

                        case TransformationType.MovementY:
                            Position.Y = t.EndFloat;
                            break;

                        case TransformationType.ParameterAdditive:
                            BlendingMode = BlendingFactorDest.One;
                            break;

                        case TransformationType.Rotation:
                            Rotation = t.EndFloat;
                            if (!RemoveOldTransformations)
                                hasRotation = true;
                            break;

                        case TransformationType.Scale:
                            Scale = new Vector2(t.EndFloat, t.EndFloat);
                            if (!RemoveOldTransformations)
                                hasScale = true;
                            break;

                        case TransformationType.VectorScale:
                            Scale = t.EndVector;
                            if (!RemoveOldTransformations)
                                hasScale = true;
                            break;
                    }

                    if (RemoveOldTransformations)
                        Transformations.RemoveAt(i--);
                    continue;
                }

                // update current transformations
                if (t.Initiated)
                {
                    switch (t.Type)
                    {
                        case TransformationType.Colour:
                            Colour = t.CurrentColour;
                            hasColour = true;
                            break;

                        case TransformationType.Fade:
                            Alpha = t.CurrentFloat;
                            hasAlpha = true;
                            break;

                        case TransformationType.Movement:
                            Position = t.CurrentVector;
                            hasMovement = true;
                            break;

                        case TransformationType.MovementX:
                            Position.X = t.CurrentFloat;
                            hasMovementX = true;
                            break;

                        case TransformationType.MovementY:
                            Position.Y = t.CurrentFloat;
                            break;

                        case TransformationType.ParameterAdditive:
                            BlendingMode = BlendingFactorDest.One;
                            break;

                        case TransformationType.Rotation:
                            Rotation = t.CurrentFloat;
                            hasRotation = true;
                            break;

                        case TransformationType.Scale:
                            Scale = new Vector2(t.CurrentFloat, t.CurrentFloat);
                            hasScale = true;
                            break;

                        case TransformationType.VectorScale:
                            Scale = t.CurrentVector;
                            hasScale = true;
                            break;
                    }

                    continue;
                }

                switch (t.Type)
                {
                    case TransformationType.Colour:
                        if (!hasColour)
                        {
                            hasColour = true;
                            Colour = t.CurrentColour;
                        }
                        break;

                    case TransformationType.Fade:
                        if (!hasAlpha)
                        {
                            hasAlpha = true;
                            Alpha = t.CurrentFloat;
                        }
                        break;

                    case TransformationType.Movement:
                        if (!hasMovement)
                            Position = t.CurrentVector;
                        break;

                    case TransformationType.MovementX:
                        if (!hasMovementX)
                            Position.X = t.CurrentFloat;
                        break;

                    case TransformationType.MovementY:
                        Position.Y = t.CurrentFloat;
                        break;

                    case TransformationType.ParameterAdditive:
                        BlendingMode = BlendingFactorDest.One;
                        break;

                    case TransformationType.Rotation:
                        if (!hasRotation)
                        {
                            hasRotation = true;
                            Rotation = t.CurrentFloat;
                        }
                        break;

                    case TransformationType.Scale:
                        if (!hasScale)
                        {
                            hasScale = true;
                            Scale = new Vector2(t.CurrentFloat, t.CurrentFloat);
                        }
                        break;

                    case TransformationType.VectorScale:
                        if (!hasScale)
                        {
                            hasScale = true;
                            Scale = t.CurrentVector;
                        }
                        break;
                }
            }
        }

        internal int ClockingNow
        {
            get { return Clock.GetTime(Clocking); }
        }

        internal void FadeIn(int duration)
        {
            int count = Transformations.Count;

            if (count == 0 && Alpha == 1)
                return;

            if (count == 1)
            {
                Transformation t = Transformations[0];
                if (t.Type == TransformationType.Fade && t.EndFloat == 1 && t.Duration == duration)
                    return;
            }

            Transformations.RemoveAll(t => t.Type == TransformationType.Fade);

            if (1 - Alpha < float.Epsilon)
                return;

            int now = Clock.GetTime(Clocking);
            Transform(new Transformation(TransformationType.Fade,
                                         Alpha, (Colour.A != 0 ? Colour.A : 1),
                                         now, now + duration));
        }

        internal void FadeInFromZero(int duration)
        {
            Transformations.RemoveAll(t => t.Type == TransformationType.Fade);

            int now = Clock.GetTime(Clocking);
            Transform(new Transformation(TransformationType.Fade,
                                         0, (Colour.A != 0 ? Colour.A : 1),
                                         now, now + duration));
        }

        internal void FadeOut(int duration)
        {
            if (Alpha == 0) return;

            int count = Transformations.Count;

            if (count == 0 && !AlwaysDraw)
                return;

            if (count == 1)
            {
                Transformation t = Transformations[0];
                if (t.Type == TransformationType.Fade && t.EndFloat == 0 && t.Duration == duration)
                    return;
            }

            Transformations.RemoveAll(t => t.Type == TransformationType.Fade);

            if (Alpha < float.Epsilon)
                return;

            int now = Clock.GetTime(Clocking);
            Transform(new Transformation(TransformationType.Fade, Alpha, 0, now, now + duration));
        }

        internal void FadeOutFromOne(int duration)
        {
            Transformations.RemoveAll(t => t.Type == TransformationType.Fade);

            int now = Clock.GetTime(Clocking);
            Transform(new Transformation(TransformationType.Fade, 1, 0, now, now + duration));
        }

        internal void FadeColour(Color4 colour, int duration)
        {
            FadeColour(colour, duration, false);
        }

        internal void FadeColour(Color4 colour, int duration, bool force)
        {
            if (!force && Colour == colour && Transformations.Count == 0)
                return;

            Transformations.RemoveAll(t => t.Type == TransformationType.Colour);

            if (duration == 0)
            {
                //Shortcut a duration of 0.
                Colour = colour;
                return;
            }

            Transform(
                      new Transformation(Colour, colour,
                                         Clock.GetTime(Clocking) - (int)GameBase.ElapsedMilliseconds,
                                         Clock.GetTime(Clocking) + duration));
        }

        internal void FlashColour(Color4 colour, int duration)
        {
            Color4 end = Colour;

            Transformation last = Transformations.FindLast(t => t.Type == TransformationType.Colour);
            if (last != null)
            {
                end = last.EndColour;
                Transformations.RemoveAll(t => t.Type == TransformationType.Colour);
            }

            Transformation flash = new Transformation(colour, end,
                                   ClockingNow,
                                   ClockingNow + duration);
            Transform(flash);
        }

        /// <summary>
        /// Moves the sprite to a specified desintation, using the current location as the source.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="easing">The easing.</param>
        internal void MoveTo(Vector2 destination, int duration, EasingTypes easing = EasingTypes.None)
        {
            Transformations.RemoveAll(t => (t.Type & TransformationType.Movement) > 0);

            if (destination == Position)
                return;

            if (duration == 0)
                Position = destination;

            int now = Clock.GetTime(Clocking);

            Transform(new Transformation(Position, destination, now, now + duration, easing));
        }

        /// <summary>
        /// Scales the sprite to a specified desintation, using the current location as the source.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="easing">The easing.</param>
        internal void ScaleTo(float target, int duration, EasingTypes easing)
        {
            Transformations.RemoveAll(t => (t.Type & TransformationType.Scale) > 0);

            if (target == ScaleScalar)
                return;

            if (duration == 0)
                ScaleScalar = target;

            int now = Clock.GetTime(Clocking);

            Transform(new Transformation(TransformationType.Scale, ScaleScalar, target, now, now + duration, easing));
        }

        #region IDrawable Members

        public virtual bool Draw()
        {
            if (Alpha != 0 &&
                (Transformations.Count != 0 || AlwaysDraw) &&
                IsOnScreen)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region IUpdateable Members

        public virtual void Update()
        {
            UpdateTransformations();
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            UnbindAllEvents();
        }

        #endregion

        #region IComparable<pDrawable> Members

        public int CompareTo(pDrawable other)
        {
            return Transformations[0].StartTime.CompareTo(other.Transformations[0].StartTime);
        }

        #endregion
    }

    internal class pDrawableDepthComparer : IComparer<pDrawable>
    {
        #region IComparer<pDrawable> Members

        public int Compare(pDrawable x, pDrawable y)
        {
            return x.DrawDepth.CompareTo(y.DrawDepth);
        }

        #endregion
    }
}
