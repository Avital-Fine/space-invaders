//*** Guy Ronen © 2008-2011 ***//
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ServiceInterfaces;
using Infrastructure.ObjectModel.Animators;
using static Infrastructure.Enums;
using System;
using Infrastructure.ObjectModel.Screens;

namespace Infrastructure.ObjectModel
{
    public class Sprite : Component2D
    {
        private Texture2D m_Texture;
        protected eCollisionBased m_CollisionBased = eCollisionBased.RectangleBased; //default
        private Color[] m_PixelsData;

        protected Vector2 m_Velocity = Vector2.Zero;
        private float m_AngularVelocity = 0;
        protected CompositeAnimator m_AnimationsManager;
        protected bool m_SaveAndRestoreDeviceState = false;

        #region get&set
        public override Vector2 Position
        {
            get { return m_Position; }
            set
            {
                if (m_Position != value)
                {
                    m_Position = value;
                    OnPositionChanged();
                    Vector2 borders = bordersCollided();
                    if (borders != Vector2.Zero)
                    {
                        OnBordersCollided(borders);
                    }
                }
            }
        }
        
        public CompositeAnimator AnimationsManager
        {
            get { return m_AnimationsManager; }
            set { m_AnimationsManager = value; }
        }

        public Texture2D Texture
        {
            get { return m_Texture; }
            set { m_Texture = value; }
        }

        public eCollisionBased CollisionBased
        {
            get { return m_CollisionBased; }
            set {
                m_CollisionBased = value;
                /*if(m_CollisionBased != value)
                {
                    m_CollisionBased = value;
                    if(m_CollisionBased == eCollisionBased.PixelsBased)
                    {
                        PixelsData = new Color[Texture.Height * Texture.Width];
                        Texture.GetData<Color>(PixelsData);
                    }
                }*/
            }
        }
        
        public Color[] PixelsData
        {
            get { return m_PixelsData; }
            set { m_PixelsData = value; }
        }

        /// <summary>
        /// Pixels per Second on 2 axis
        /// </summary>
        public Vector2 Velocity
        {
            get { return m_Velocity; }
            set { m_Velocity = value; }
        }
       
        /// <summary>
        /// Radians per Second on X Axis
        /// </summary>
        public float AngularVelocity
        {
            get { return m_AngularVelocity; }
            set { m_AngularVelocity = value; }
        }

        #endregion

        public Sprite(string i_AssetName, GameScreen i_GameScreen)
            : base(i_AssetName, i_GameScreen, int.MaxValue)
        { }

        public Sprite(string i_AssetName, Game i_Game)
            : base(i_AssetName, i_Game)
        { }

        /// <summary>
        /// Default initialization of bounds
        /// </summary>
        /// <remarks>
        /// Derived classes are welcome to override this to implement their specific boudns initialization
        /// </remarks>
        public override void Initialize()
        {
            base.Initialize();

            m_AnimationsManager = new CompositeAnimator(this);
            InitAnimations();
        }

        protected override void InitBounds()
        {
            m_WidthBeforeScale = Texture.Width;
            m_HeightBeforeScale = Texture.Height;
            base.InitBounds();
        }

        protected virtual void InitAnimations()
        {
            m_AnimationsManager.Enabled = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            m_Texture = Game.Content.Load<Texture2D>(m_AssetName);

            if(CollisionBased == eCollisionBased.PixelsBased)
            {
                PixelsData = new Color[Texture.Height * Texture.Width];
                Texture.GetData<Color>(PixelsData);
            }
        }

        /// <summary>
        /// Basic movement logic (position += velocity * totalSeconds)
        /// </summary>
        /// <param name="gameTime"></param>
        /// <remarks>
        /// Derived classes are welcome to extend this logic.
        /// </remarks>
        public override void Update(GameTime gameTime)
        {
            float totalSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            this.Position += this.Velocity * totalSeconds;
            this.Rotation += this.AngularVelocity * totalSeconds;

            base.Update(gameTime);

            this.AnimationsManager.Update(gameTime);
        }

        class DeviceStates
        {
            public BlendState BlendState;
            public SamplerState SamplerState;
            public DepthStencilState DepthStencilState;
            public RasterizerState RasterizerState;
        }

        DeviceStates m_SavedDeviceStates = new DeviceStates();
        protected void saveDeviceStates()
        {
            m_SavedDeviceStates.BlendState = GraphicsDevice.BlendState;
            m_SavedDeviceStates.SamplerState = GraphicsDevice.SamplerStates[0];
            m_SavedDeviceStates.DepthStencilState = GraphicsDevice.DepthStencilState;
            m_SavedDeviceStates.RasterizerState = GraphicsDevice.RasterizerState;
        }

        private void restoreDeviceStates()
        {
            GraphicsDevice.BlendState = m_SavedDeviceStates.BlendState;
            GraphicsDevice.SamplerStates[0] = m_SavedDeviceStates.SamplerState;
            GraphicsDevice.DepthStencilState = m_SavedDeviceStates.DepthStencilState;
            GraphicsDevice.RasterizerState = m_SavedDeviceStates.RasterizerState;
        }

        public bool SaveAndRestoreDeviceState
        {
            get { return m_SaveAndRestoreDeviceState; }
            set { m_SaveAndRestoreDeviceState = value; }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!m_UseSharedBatch)
            {
                if (SaveAndRestoreDeviceState)
                {
                    saveDeviceStates();
                }

                m_SpriteBatch.Begin(
                    SortMode, BlendState, SamplerState,
                    DepthStencilState, RasterizerState, Shader, TransformMatrix);
            }

            m_SpriteBatch.Draw(m_Texture, PositionForDraw,
                 this.SourceRectangle, this.TintColor,
                this.Rotation, this.RotationOrigin, this.Scales,
                SpriteEffects.None, this.LayerDepth);

            if (!m_UseSharedBatch)
            {
                m_SpriteBatch.End();

                if (SaveAndRestoreDeviceState)
                {
                    restoreDeviceStates();
                }
            }

            base.Draw(gameTime);
        }

        #region Collision Handlers
        protected override void DrawBoundingBox()
        {
            // not implemented yet
        }

        public virtual bool CheckCollision(ICollidable i_Source)
        {
            bool collided = false;
            ICollidable2D source = i_Source as ICollidable2D;

            if (source != null)
            {
                collided = rectangleBasedCollision(source);
                if (CollisionBased == eCollisionBased.PixelsBased && collided
                   && (source as Sprite).CollisionBased == eCollisionBased.PixelsBased)
                {
                    collided = pixelsBasedCollision(source);
                }
            }

            return collided;
        }

        private bool rectangleBasedCollision(ICollidable2D i_Source)
        {
            return i_Source.Bounds.Intersects(this.Bounds);
        }

        private bool pixelsBasedCollision(ICollidable2D i_Source)
        {
            bool collided = false;
            Sprite source = i_Source as Sprite;

            if (source != null)
            {
                int leftLimit = Math.Max(this.Bounds.X, source.Bounds.X);
                int rightLimit = Math.Min(this.Bounds.X + this.Bounds.Width, source.Bounds.X + source.Bounds.Width);
                int topLimit = Math.Max(this.Bounds.Y, source.Bounds.Y);
                int downLimit = Math.Min(this.Bounds.Y + this.Bounds.Height, source.Bounds.Y + source.Bounds.Height);

                for (int y = topLimit; y < downLimit && !collided; ++y)
                {
                    for (int x = leftLimit; x < rightLimit && !collided; ++x)
                    {
                        Color thisPixel = GetPixelAt(y - this.Bounds.Y, x - this.Bounds.X);
                        Color sourcePixel = source.GetPixelAt(y - source.Bounds.Y, x - source.Bounds.X);
                        collided = thisPixel.A != 0 && sourcePixel.A != 0;
                    }
                }
            }

            return collided;
        }

        protected Vector2 GetPixelPosition(Vector2 i_Offset)
        {
            return TopLeftPosition + i_Offset;
        }

        protected Color GetPixelAt(int i_Row, int i_Col)
        {
            return PixelsData[i_Row * Texture.Width + i_Col];
        }

        protected void SetPixelAt(int i_Row, int i_Col, Color i_Color)
        {
            PixelsData[i_Row * Texture.Width + i_Col] = i_Color;
        }

        protected void ErasePixel(int i_Row, int i_Col)
        {
            Color newPixel = GetPixelAt(i_Row, i_Col);
            newPixel.A = 0;
            SetPixelAt(i_Row, i_Col, newPixel);
        }

        public virtual void Collided(ICollidable i_Collidable)
        {
            // defualt behavior - change direction:
            this.Velocity *= -1;
        }
        #endregion //Collision Handlers

        private Vector2 bordersCollided()
        {
            Vector2 retVal = Vector2.Zero;
            bool reachTheLowerBorder = TopLeftPosition.Y + Height >= Game.GraphicsDevice.Viewport.Height;
            bool reachTheUpperBorder = TopLeftPosition.Y < 0;
            bool reachTheRightBorder = TopLeftPosition.X + Width >= Game.GraphicsDevice.Viewport.Width;
            bool reachTheLeftBorder = TopLeftPosition.X < 0;
            retVal.Y = reachTheLowerBorder ? 1 : reachTheUpperBorder ? -1 : 0;
            retVal.X = reachTheRightBorder ? 1 : reachTheLeftBorder ? -1 : 0;

            return retVal;
        }

        public Sprite ShallowClone()
        {
            return this.MemberwiseClone() as Sprite;
        }
    }
}