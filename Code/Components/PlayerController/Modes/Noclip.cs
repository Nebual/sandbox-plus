namespace SandboxPlus.Movement;

[Icon( "flight" ), Group( "Movement" ), Title( "MoveMode - Noclip" )]
public class MoveModeNoclip : MoveMode
{
	[Property]
	public int Priority { get; set; } = 100;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "noclip" ) ) Tags.Toggle( "noclip" );
	}

	public override int Score( PlayerController controller )
	{
		if ( Tags.Has( "noclip" ) ) return Priority;
		return -100;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		Controller.ColliderObject.Enabled = !Controller.Tags.Has( "noclip" );

		body.Gravity = !Controller.Tags.Has( "noclip" );
		body.Velocity = Controller.WishVelocity;
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		// don't normalize, because analog input might want to go slow
		input = input.ClampLength( 1 );

		var wishVelocity = eyes * input * Controller.RunSpeed;

		if ( Input.Down( "jump" ) )
		{
			wishVelocity += Vector3.Up * Controller.JumpSpeed;
		}

		if ( Input.Down( "run" ) ) wishVelocity *= 5.0f;
		if ( Input.Down( "duck" ) ) wishVelocity *= 0.2f;

		return wishVelocity;
	}

	public override void OnModeBegin()
	{
		Controller.Renderer.Set( "b_noclip", true );
	}

	public override void OnModeEnd( MoveMode next )
	{
		Controller.Renderer.Set( "b_noclip", false );
	}
}
