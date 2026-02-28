using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ObjectModel.Screens;

namespace Infrastructure.ObjectModel
{
    public class Text : Component2D
    {
        public const string k_FontsPath = "Fonts/";
        private SpriteFont m_Font;
        private string m_Content = string.Empty;

        public string Content 
        { 
            get { return m_Content; } 
            set 
            {
                m_Content = value;
                contentChanged();
            } 
        }

        public Text(GameScreen i_GameScreen, string i_FileName)
            : base(k_FontsPath + i_FileName, i_GameScreen)
        {
        }

        public Text(GameScreen i_GameScreen, string i_FileName, string i_Content)
            : this(i_GameScreen, i_FileName)
        {
            m_Content = i_Content;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            m_Font = Game.Content.Load<SpriteFont>(AssetName);
        }

        protected override void InitBounds()
        {
             WidthBeforeScale = m_Font.MeasureString(m_Content).X;
             HeightBeforeScale = m_Font.MeasureString(m_Content).Y;

            base.InitBounds();
        }

        public override void Draw(GameTime i_GameTime)
        {
            if (!m_UseSharedBatch)
            {
                m_SpriteBatch.Begin();
            }

            m_SpriteBatch.DrawString(m_Font, m_Content, PositionForDraw, this.TintColor, this.Rotation, this.RotationOrigin, this.Scales, this.SpriteEffects, this.LayerDepth);

            if (!m_UseSharedBatch)
            {
                m_SpriteBatch.End();
            }

            base.Draw(i_GameTime);
        }

        private void contentChanged()
        {
            if (m_Font != null)
                {
                    WidthBeforeScale = m_Font.MeasureString(m_Content).X;
                    HeightBeforeScale = m_Font.MeasureString(m_Content).Y;
                    InitSourceRectangle();
                }
        }
    }
}
