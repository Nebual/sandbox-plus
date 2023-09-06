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

		spawned.OnBreak += () =>
		{
			jointTracker1.Joints.Remove( spawned );
			jointTracker2.Joints.Remove( spawned );
		};
	}

	// ent.PhysicsGroup.Joints only appears to work for ModelDoc joints (eg. within a ragdoll), not for PhysicsJoint.Create'd ones, so lets track it ourselves
	public static List<PhysicsJoint> GetJoints( this Entity ent )
	{
		Game.AssertServer();
		var jointTracker = ent.Components.Get<JointTrackerComponent>();
		if ( jointTracker is not null )
		{
			// Due to https://github.com/sboxgame/issues/issues/3949 OnBreak isn't called on Joint.Remove(), so we need to clean up here just in case
			jointTracker.Joints.RemoveAll( x => !x.IsValid() );
			return jointTracker.Joints;
		}
		return new();
	}

	public static Sandbox.Tools.ConstraintType GetConstraintType( this PhysicsJoint joint )
	{
		if ( joint is FixedJoint fixedJoint )
		{
			if ( !fixedJoint.EnableAngularConstraint && !fixedJoint.EnableLinearConstraint )
			{
				return Sandbox.Tools.ConstraintType.Nocollide;
			}
			return Sandbox.Tools.ConstraintType.Weld;
		}
		else if ( joint is HingeJoint )
		{
			return Sandbox.Tools.ConstraintType.Axis;
		}
		else if ( joint is BallSocketJoint )
		{
			return Sandbox.Tools.ConstraintType.BallSocket;
		}
		else if ( joint is SpringJoint springJoint )
		{
			if ( springJoint.MinLength <= 0.001f )
			{
				return Sandbox.Tools.ConstraintType.Rope;
			}
			return Sandbox.Tools.ConstraintType.Spring;
		}
		else if ( joint is SliderJoint )
		{
			return Sandbox.Tools.ConstraintType.Slider;
		}
		throw new System.Exception( "Unknown joint type" );
	}
}

public class JointTrackerComponent : EntityComponent
{
	public List<PhysicsJoint> Joints { get; set; } = new();
}
