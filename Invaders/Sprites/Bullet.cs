using System;
using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;
using Infrastructure.ServiceInterfaces;
using Infrastructure.ObjectModel.Screens;
using Invaders.Interfaces;
using static Infrastructure.Enums;

namespace Invaders.Sprites
{
	public class Bullet : Sprite, ICollidable2D
	{
		public event EventHandler<EventArgs> RouteFinished;
		public event Action<IHostile> HostileCollided;

		private const string k_AssertName = @"Sprites\Bullet";
		private const int k_RandomRange = 2;
		private readonly Vector2 r_Velocity = new Vector2(0, 140);
		private readonly Random r_RandomMachine = new Random();
		private readonly eBulletType r_Type;
		private bool m_FallingDown;
		private bool m_Active;

		public bool FallingDown { get { return m_FallingDown; } }
		public eBulletType Type { get { return r_Type; } }

		public bool Active 
		{ 
			get { return m_Active; } 
			set
			{
				if (m_Active != value)
				{
					m_Active = value;

					if (m_Active == false)
					{
						onRouteFinished();
					}
				}	
			} 
		}

		public Bullet(GameScreen i_GameScreen, eBulletType i_Type)
			: base(k_AssertName, i_GameScreen)
		{
			r_Type = i_Type;
			CollisionBased = eCollisionBased.PixelsBased;
		}

        public override void Initialize()
        {
            base.Initialize();
			
			m_FallingDown = r_Type == eBulletType.Enemy;
			TintColor = m_FallingDown ? Color.Blue : Color.Red;
			Velocity = m_FallingDown ? r_Velocity : (-1) * r_Velocity;
			Deactivate();
			BordersCollided += Bullet_BordersCollided;
        }
		
		public void Shoot(Vector2 i_Position)
		{
			Position = i_Position;
			Activate();
		}

		public override void Collided(ICollidable i_Collidable)
        {
            if (this.Type == eBulletType.Ship && i_Collidable is IHostile)
            {
				Deactivate();
				onHostileCollided(i_Collidable as IHostile);
			}
			else if (i_Collidable is Bullet && this.Type != (i_Collidable as Bullet).Type) 
            {
				if (this.Type == eBulletType.Enemy)
				{
					randomDeactivate();
				}
                else
                {
					Deactivate();
                }
            }
		}

        private void randomDeactivate()
        {
			if (r_RandomMachine.Next(k_RandomRange) == 0)
			{
				Deactivate();
			}
		}

		public void Deactivate()
		{
			Active = false;
			Enabled = false;
			Visible = false;
		}

		public void Activate()
		{
			Active = true;
			Enabled = true;
			Visible = true;
		}

		private void Bullet_BordersCollided(object sender, Vector2 i_BordersCollided)
		{
			if (i_BordersCollided.Y != 0)
			{
				Deactivate();
			}
		}

		private void onRouteFinished()
		{
			if (RouteFinished != null)
			{
				RouteFinished.Invoke(this, EventArgs.Empty);
			}
		}

		private void onHostileCollided(IHostile i_Hostile)
		{
			if (HostileCollided != null)
			{
				HostileCollided.Invoke(i_Hostile);
			}
		}
	}
}
