namespace SandboxPlus.Movement;

// useful for the Wire Keyboard, or anytime you want to glue yourself to a prop to surf
[Group( "Movement" ), Title( "MoveMode - Locked Position" )]
public class LockedPositionMode : MoveMode
{
	[Property]
	public int Priority { get; set; } = 110;
	public Action<Rotation, Vector3> OnInput;

	private Vector3 GroundLocalPos;
	private GameObject lastGroundObject;

	public override int Score( PlayerController controller )
	{
		if ( Tags.Has( "lockedposition" ) ) return Priority;
		return -100;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		// Controller.ColliderObject.Enabled = !Controller.Tags.Has( "lockedposition" );

		if ( Controller.GroundObject.IsValid() && !lastGroundObject.IsValid() )
		{
			lastGroundObject = Controller.GroundObject;
			GroundLocalPos = Controller.GroundObject.Transform.World.PointToLocal( Controller.GameObject.WorldPosition );
		}
		if ( lastGroundObject.IsValid() && GroundLocalPos.LengthSquared > 0 )
		{
			Controller.GameObject.WorldPosition = lastGroundObject.Transform.World.PointToWorld( GroundLocalPos );
			if ( lastGroundObject.GetComponent<Rigidbody>() is Rigidbody groundBody )
			{
				body.Velocity = groundBody.Velocity;
			}
		}
	}
	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		// don't normalize, because analog input might want to go slow
		input = input.ClampLength( 1 );
		OnInput?.Invoke( eyes, input );
		return Vector3.Zero;
	}

	public override void OnModeBegin()
	{
	}
	public override void OnModeEnd( MoveMode next )
	{
		GroundLocalPos = Vector3.Zero;
		lastGroundObject = null;
	}
}
