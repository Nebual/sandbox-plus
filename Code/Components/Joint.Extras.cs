using Sandbox.Physics;
public static class JointExtensions
{
	// [Event( "joint.spawned" )]
	public static void OnJointSpawned( PhysicsJoint spawned, GameObject owner )
	{
		var jointTracker1 = spawned.Body1.GetGameObject().GetComponent<PropHelper>();
		jointTracker1.PhysicsJoints.Add( spawned );
		var jointTracker2 = spawned.Body2.GetGameObject().GetComponent<PropHelper>();
		jointTracker2.PhysicsJoints.Add( spawned );

		spawned.OnBreak += () =>
		{
			jointTracker1.PhysicsJoints.Remove( spawned );
			jointTracker2.PhysicsJoints.Remove( spawned );
		};
	}

	// ent.PhysicsGroup.Joints only appears to work for ModelDoc joints (eg. within a ragdoll), not for PhysicsJoint.Create'd ones, so lets track it ourselves
	public static List<PhysicsJoint> GetJoints( this GameObject ent )
	{
		var jointTracker = ent.GetComponent<PropHelper>();
		if ( jointTracker is not null )
		{
			// Due to https://github.com/sboxgame/issues/issues/3949 OnBreak isn't called on Joint.Remove(), so we need to clean up here just in case
			jointTracker.PhysicsJoints.RemoveAll( x => !x.IsValid() );
			return jointTracker.PhysicsJoints;
		}
		return [];
	}

	public static Sandbox.Tools.ConstraintType GetConstraintType( this Sandbox.Physics.PhysicsJoint joint )
	{
		if ( joint is Sandbox.Physics.FixedJoint )
		{
			return Sandbox.Tools.ConstraintType.Weld;
		}
		else if ( joint is Sandbox.Physics.HingeJoint )
		{
			return Sandbox.Tools.ConstraintType.Axis;
		}
		else if ( joint is Sandbox.Physics.BallSocketJoint )
		{
			return Sandbox.Tools.ConstraintType.BallSocket;
		}
		else if ( joint is Sandbox.Physics.SpringJoint springJoint )
		{
			if ( springJoint.MinLength <= 0.001f )
			{
				if ( springJoint.MaxLength == 9999999 )
				{
					return Sandbox.Tools.ConstraintType.Nocollide;
				}
				return Sandbox.Tools.ConstraintType.Rope;
			}
			return Sandbox.Tools.ConstraintType.Spring;
		}
		else if ( joint is Sandbox.Physics.SliderJoint )
		{
			return Sandbox.Tools.ConstraintType.Slider;
		}
		throw new System.Exception( "Unknown joint type" );
	}

	public static bool IsWorld( this GameObject gameObject )
	{
		return gameObject.GetComponent<MapCollider>() != null;
	}
}
