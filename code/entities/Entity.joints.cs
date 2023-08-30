using System.Collections.Generic;
using Sandbox;
using Sandbox.Physics;

public static class EntityJointsExtensions
{
	[Event( "joint.spawned" )]
	public static void OnJointSpawned( PhysicsJoint spawned, Entity owner )
	{
		var jointTracker1 = spawned.Body1.GetEntity().Components.GetOrCreate<JointTrackerComponent>();
		jointTracker1.Joints.Add( spawned );
		var jointTracker2 = spawned.Body2.GetEntity().Components.GetOrCreate<JointTrackerComponent>();
		jointTracker2.Joints.Add( spawned );
	}

	// ent.PhysicsGroup.Joints only appears to work for ModelDoc joints (eg. within a ragdoll), not for PhysicsJoint.Create'd ones, so lets track it ourselves
	public static List<PhysicsJoint> GetJoints( this Entity ent )
	{
		Game.AssertServer();
		var jointTracker = ent.Components.Get<JointTrackerComponent>();
		if ( jointTracker is not null )
		{
			return jointTracker.Joints;
		}
		return new();
	}
}

public class JointTrackerComponent : EntityComponent
{
	public List<PhysicsJoint> Joints { get; set; } = new();
}
