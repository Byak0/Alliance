using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	/// <summary>
	/// Advanced combat mechanics for the Ent.
	/// </summary>
	public class EntComponent : AL_DefaultAgentComponent
	{
		private Vec3[] _previousBonePositions;
		private bool[] _isBoneStomping;
		private Dictionary<Agent, float> _lastProjections = new Dictionary<Agent, float>();

		public EntComponent(Agent agent) : base(agent)
		{
			_previousBonePositions = new Vec3[EntConstants.StompCollisionBoneIds.Count];
			_isBoneStomping = new bool[EntConstants.StompCollisionBoneIds.Count];
		}

		public override void OnTickAsAI(float dt)
		{
		}

		public override void OnTick(float dt)
		{
			UpdateStompState();
			CheckForBonesCollisions();
		}

		// Check if the bones in motion are colliding with agents
		private void CheckForBonesCollisions()
		{
			Skeleton agentSkeleton = Agent.AgentVisuals?.GetSkeleton();

			// Iterate through the bones in motion
			for (int i = 0; i < EntConstants.StompCollisionBoneIds.Count; i++)
			{
				if (_isBoneStomping[i])
				{
					// Get the list of agents near the bone
					MatrixFrame boneLocalFrame = agentSkeleton.GetBoneEntitialFrameWithIndex(EntConstants.StompCollisionBoneIds[i]);
					Vec3 boneGlobalPosition = Agent.AgentVisuals.GetGlobalFrame().TransformToParent(boneLocalFrame.origin);
					Mat3 boneGlobalRotation = Agent.AgentVisuals.GetGlobalFrame().rotation.TransformToParent(boneLocalFrame.rotation);
					Vec3 bonePosition = new MatrixFrame(boneGlobalRotation, boneGlobalPosition).origin;

					List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(EntConstants.STOMP_RADIUS, bonePosition);

					// Iterate through the agents
					foreach (Agent nearbyAgent in nearbyAgents)
					{
						if (nearbyAgent != Agent && nearbyAgent.IsActive() && !nearbyAgent.IsEnt() && !nearbyAgent.IsTroll())
						{
							// Check if agent collided recently and ignore it
							if (_lastProjections.ContainsKey(nearbyAgent) && _lastProjections[nearbyAgent] > Mission.Current.CurrentTime - 3f)
							{
								continue;
							}
							else
							{
								_lastProjections[nearbyAgent] = Mission.Current.CurrentTime;
								float distance = (nearbyAgent.Position - bonePosition).Length;
								// Calculate the force of the projection (maximum at range 0)
								float force = MathF.Max(0f, 1 - distance / EntConstants.STOMP_RADIUS);
								nearbyAgent.ProjectAgent(bonePosition, force);
								// Deal damage based on the force
								//Agent.DealDamage(nearbyAgent, (int)(force * EntConstants.BASE_STOMP_DAMAGE));
								Log($"{Agent.Name} collided with {nearbyAgent.Name} at {_lastProjections[nearbyAgent]}", LogLevel.Debug);
							}
						}
					}
				}
			}
		}



		// Check if the bones are stomping
		private void UpdateStompState()
		{
			Skeleton skeleton = Agent.AgentVisuals.GetSkeleton();
			if (skeleton == null)
			{
				Log($"Failed to get agents visuals or skeleton for {Agent.Name}", LogLevel.Error);
				return;
			}
			// Check if the bones are stomping (position changed by more than 0.1f since last tick, downward movement and reached ground)
			for (int i = 0; i < EntConstants.StompCollisionBoneIds.Count; i++)
			{
				Vec3 bonePosition = skeleton.GetBoneEntitialFrameWithIndex(EntConstants.StompCollisionBoneIds[i]).origin;
				float groundHeight = Mission.Current.Scene.GetGroundHeightAtPosition(bonePosition);
				if (bonePosition.Z < _previousBonePositions[i].Z - 0.01f
					&& groundHeight > bonePosition.Z)
				{
					_isBoneStomping[i] = true;
				}
				else
				{
					_isBoneStomping[i] = false;
				}
				_previousBonePositions[i] = bonePosition;
			}
		}
	}
}
