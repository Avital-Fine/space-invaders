//*** Guy Ronen © 2008-2011 ***//
using System;
using Microsoft.Xna.Framework;

namespace Infrastructure.ObjectModel.Animators.ConcreteAnimators
{
    public class WaypointsAnymator : SpriteAnimator
    {
        private float m_VelocityPerSecond;
        private Vector2[] m_Waypoints;
        private int m_CurrentWaypoint = 0;
        private bool m_Loop = false;

        // CTORs
        public WaypointsAnymator(
            float i_VelocityPerSecond,
            TimeSpan i_AnimationLength,
            bool i_Loop,
            params Vector2[] i_Waypoints)

            : this("Waypoints", i_VelocityPerSecond, i_AnimationLength, i_Loop, i_Waypoints)
        {}

        public WaypointsAnymator(
            string i_Name,
            float i_VelocityPerSecond,
            TimeSpan i_AnimationLength,
            bool i_Loop,
            params Vector2[] i_Waypoints)

            : base(i_Name, i_AnimationLength)
        {
            this.m_VelocityPerSecond = i_VelocityPerSecond;
            this.m_Waypoints = i_Waypoints;
            m_Loop = i_Loop;
            m_ResetAfterFinish = false;
        }

        protected override void RevertToOriginal()
        {
            this.BoundSprite.Position = m_OriginalSpriteInfo.Position;
        }

        protected override void DoFrame(GameTime i_GameTime)
        {
            // This offset is how much we need to move based on how much time 
            // has elapsed.
            float maxDistance = (float)i_GameTime.ElapsedGameTime.TotalSeconds * m_VelocityPerSecond;

            // The vector that is left to get to the current waypoint
            Vector2 remainingVector = m_Waypoints[m_CurrentWaypoint] - this.BoundSprite.Position;
            if (remainingVector.Length() > maxDistance)
            {
                // The vector is longer than we can travel,
                // so limit to our maximum travel distance
                remainingVector.Normalize();
                remainingVector *= maxDistance;
            }

            // Move
            this.BoundSprite.Position += remainingVector;

            if (reachedCurrentWaypoint())
            {
                lookAtNextWayPoint();
            }
        }

        private void lookAtNextWayPoint()
        {
            if (reachedLastWaypoint() && !m_Loop)
            {
                // No more waypoints, so this animation is finished
                base.IsFinished = true;
            }
            else
            {
                // We have more waypoints to go. NEXT!
                m_CurrentWaypoint++;
                m_CurrentWaypoint %= m_Waypoints.Length;
            }
        }

        private bool reachedLastWaypoint()
        {
            return (m_CurrentWaypoint == m_Waypoints.Length - 1);
        }

        private bool reachedCurrentWaypoint()
        {
            return (this.BoundSprite.Position == m_Waypoints[m_CurrentWaypoint]);
        }
    }
}
