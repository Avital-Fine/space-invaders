using Infrastructure.ObjectModel.Animators;
using Infrastructure.ObjectModel.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.ObjectModel
{
    public class Component2D : LoadableDrawableComponent
    {
        protected BaseGame m_BaseGame;
        protected float m_WidthBeforeScale;
        protected float m_HeightBeforeScale;
        protected Vector2 m_Position = Vector2.Zero;
        public Vector2 m_PositionOrigin;
        public Vector2 m_RotationOrigin = Vector2.Zero;
        protected Rectangle m_SourceRectangle;
        protected float m_Rotation = 0;
        protected Vector2 m_Scales = Vector2.One;
        protected Color m_TintColor = Color.White;
        protected float m_LayerDepth;
        protected SpriteEffects m_SpriteEffects = SpriteEffects.None;
        protected bool m_UseSharedBatch = false;
        protected SpriteBatch m_SpriteBatch;
        protected BlendState m_BlendState = BlendState.AlphaBlend;
        protected SpriteSortMode m_SortMode = SpriteSortMode.Deferred;
        protected SamplerState m_SamplerState = null;
        protected DepthStencilState m_DepthStencilState = null;
        protected RasterizerState m_RasterizerState = null;
        protected Matrix m_TransformMatrix = Matrix.Identity;
        protected Effect m_Shader = null;

        #region get&set
        public BaseGame BaseGame
        {
            get { return (this.Game as BaseGame); }
        }

        public float Width
        {
            get { return m_WidthBeforeScale * m_Scales.X; }
            set { m_WidthBeforeScale = value / m_Scales.X; }
        }

        public float Height
        {
            get { return m_HeightBeforeScale * m_Scales.Y; }
            set { m_HeightBeforeScale = value / m_Scales.Y; }
        }

        public float WidthBeforeScale
        {
            get { return m_WidthBeforeScale; }
            set { m_WidthBeforeScale = value; }
        }


        public float HeightBeforeScale
        {
            get { return m_HeightBeforeScale; }
            set { m_HeightBeforeScale = value; }
        }

        /// <summary>
        /// Represents the location of the sprite's origin point in screen coorinates
        /// </summary>
        public virtual Vector2 Position
        {
            get { return m_Position; }
            set { m_Position = value;}
        }

        public Vector2 PositionOrigin
        {
            get { return m_PositionOrigin; }
            set { m_PositionOrigin = value; }
        }

        public Vector2 RotationOrigin
        {
            get { return m_RotationOrigin; }// r_SpriteParameters.RotationOrigin; }
            set { m_RotationOrigin = value; }
        }

        protected Vector2 PositionForDraw
        {
            get { return this.Position - this.PositionOrigin + this.RotationOrigin; }
        }

        public Vector2 TopLeftPosition
        {
            get { return this.Position - this.PositionOrigin; }
            set { this.Position = value + this.PositionOrigin; }
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(
                    (int)TopLeftPosition.X,
                    (int)TopLeftPosition.Y,
                    (int)this.Width,
                    (int)this.Height);
            }
        }

        public Rectangle BoundsBeforeScale
        {
            get
            {
                return new Rectangle(
                    (int)TopLeftPosition.X,
                    (int)TopLeftPosition.Y,
                    (int)this.WidthBeforeScale,
                    (int)this.HeightBeforeScale);
            }
        }


        public Rectangle SourceRectangle
        {
            get { return m_SourceRectangle; }
            set { m_SourceRectangle = value; }
        }

        public Vector2 TextureCenter
        {
            get
            {
                return new Vector2((float)(Width / 2), (float)(Height / 2));
            }
        }

        public Vector2 SourceRectangleCenter
        {
            get { return new Vector2((float)(m_SourceRectangle.Width / 2), (float)(m_SourceRectangle.Height / 2)); }
        }


        public float Rotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }


        public Vector2 Scales
        {
            get { return m_Scales; }
            set
            {
                if (m_Scales != value)
                {
                    m_Scales = value;
                    // Notify the Collision Detection mechanism:
                    OnPositionChanged();
                }
            }
        }


        public Color TintColor
        {
            get { return m_TintColor; }
            set { m_TintColor = value; }
        }

        public float Opacity
        {
            get { return (float)m_TintColor.A / (float)byte.MaxValue; }
            set { m_TintColor.A = (byte)(value * (float)byte.MaxValue); }
        }

        public float LayerDepth
        {
            get { return m_LayerDepth; }
            set { m_LayerDepth = value; }
        }


        public SpriteEffects SpriteEffects
        {
            get { return m_SpriteEffects; }
            set { m_SpriteEffects = value; }
        }

        public SpriteBatch SpriteBatch
        {
            set
            {
                m_SpriteBatch = value;
                m_UseSharedBatch = true;
            }
        }

        public SpriteSortMode SortMode
        {
            get { return m_SortMode; }
            set { m_SortMode = value; }
        }

        public BlendState BlendState
        {
            get { return m_BlendState; }
            set { m_BlendState = value; }
        }

        public SamplerState SamplerState
        {
            get { return m_SamplerState; }
            set { m_SamplerState = value; }
        }

        public DepthStencilState DepthStencilState
        {
            get { return m_DepthStencilState; }
            set { m_DepthStencilState = value; }
        }

        public RasterizerState RasterizerState
        {
            get { return m_RasterizerState; }
            set { m_RasterizerState = value; }
        }

        public Effect Shader
        {
            get { return m_Shader; }
            set { m_Shader = value; }
        }

        public Matrix TransformMatrix
        {
            get { return m_TransformMatrix; }
            set { m_TransformMatrix = value; }
        }

        #endregion

        #region constructors

        public Component2D(string i_AssetName, GameScreen i_GameScreen, int i_UpdateOrder, int i_DrawOrder)
            : base(i_AssetName, i_GameScreen, i_UpdateOrder, i_DrawOrder)
        { }

        public Component2D(string i_AssetName, GameScreen i_GameScreen, int i_CallsOrder)
            : base(i_AssetName, i_GameScreen, i_CallsOrder)
        { }

        public Component2D(string i_AssetName, GameScreen i_GameScreen)
            : base(i_AssetName, i_GameScreen, int.MaxValue)
        {
        }

        public Component2D(string i_AssetName, Game i_Game)
            : base(i_AssetName, i_Game, int.MaxValue)
        { }

        #endregion

            #region Initialize

        protected override void LoadContent()
        {
            base.LoadContent();

            if (m_SpriteBatch == null)
            {
                m_SpriteBatch =
                    Game.Services.GetService(typeof(SpriteBatch)) as SpriteBatch;

                if (m_SpriteBatch == null)
                {
                    m_SpriteBatch = new SpriteBatch(Game.GraphicsDevice);
                    m_UseSharedBatch = false;
                }
            }
        }

        protected override void InitBounds()
        {
            m_Position = Vector2.Zero;

            InitSourceRectangle();
            InitOrigins();
        }

        protected virtual void InitOrigins()
        {
        }

        protected virtual void InitSourceRectangle()
        {
            m_SourceRectangle = new Rectangle(0, 0, (int)m_WidthBeforeScale, (int)m_HeightBeforeScale);
        }

        public void InitOriginsToCenter()
        {
            PositionOrigin = SourceRectangleCenter;
            RotationOrigin = SourceRectangleCenter;
        }

        public void Centralize()
        {
            InitOriginsToCenter();
            this.Position = GameScreen.CenterOfViewPort;
        }

        #endregion

        protected override void DrawBoundingBox()
        {
            //throw new NotImplementedException();
        }
    }
}
