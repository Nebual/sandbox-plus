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

	public static IEnumerable<GameObject> GetAttachedGameObjects( this GameObject baseGo, List<PhysicsJoint> joints = null )
	{
		joints ??= new();
		HashSet<GameObject> objsChecked = new();
		HashSet<PhysicsJoint> jointsChecked = joints.ToHashSet();
		Stack<GameObject> objsToCheck = new();
		objsChecked.Add( baseGo );
		objsToCheck.Push( baseGo );

		while ( objsToCheck.Count > 0 )
		{
			GameObject ent = objsToCheck.Pop();
			foreach ( GameObject e in ent.Children )
			{
				if ( objsChecked.Add( e ) )
					objsToCheck.Push( e );
			}
			if ( ent.Parent.IsValid() && ent.Parent.GetComponent<Prop>() is not null && objsChecked.Add( ent.Parent ) )
			{
				objsToCheck.Push( ent.Parent );
			}

			// if ( ent.GetComponent<SkinnedModelRenderer>() is SkinnedModelRenderer ragdoll )
			// {
			// 	for ( int i = 0, end = ragdoll.Model.BoneCount; i < end; ++i )
			// 	{
			// 		GameObject e = ragdoll.GetBoneObject( i );
			// 		if ( objsChecked.Add( e ) )
			// 			objsToCheck.Push( e );
			// 	}
			// }
			foreach ( PhysicsJoint j in GetJoints( ent ) )
			{
				if ( jointsChecked.Add( j ) )
				{
					if ( objsChecked.Add( j.Body1.GetGameObject() ) )
						objsToCheck.Push( j.Body1.GetGameObject() );
					if ( objsChecked.Add( j.Body2.GetGameObject() ) )
						objsToCheck.Push( j.Body2.GetGameObject() );
				}
			}
		}
		joints.AddRange( jointsChecked );
		return objsChecked;
	}

	public static IEnumerable<T> GetAttachedGameObjects<T>( this GameObject baseGo, List<PhysicsJoint> joints = null )
	{
		return baseGo.GetAttachedGameObjects( joints ).Select( x => x.GetComponent<T>() ).Where( x => x is not null );
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
