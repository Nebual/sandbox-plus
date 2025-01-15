[Library( "tool_physicslocking", Title = "Physics Locking", Description = "Keep upright, keep flat, keep stationary, etc", Group = "constraints" )]
public class PhysicsLocking : BaseTool
{
	[Property, Title( "Pitch Lock" )]
	public bool PitchLock { get; set; } = true;
	[Property, Title( "Yaw Lock" )]
	public bool YawLock { get; set; } = false;
	[Property, Title( "Roll Lock" )]
	public bool RollLock { get; set; } = true;
	[Property, Title( "X Lock" )]
	public bool XLock { get; set; } = false;
	[Property, Title( "Y Lock" )]
	public bool YLock { get; set; } = false;
	[Property, Title( "Z Lock" )]
	public bool ZLock { get; set; } = false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) )
			return false;
		if ( !trace.Hit )
			return false;
		var go = trace.GameObject;

		SetPhysicsLocking( go, new PhysicsLock()
		{
			Pitch = PitchLock,
			Yaw = YawLock,
			Roll = RollLock,
			X = XLock,
			Y = YLock,
			Z = ZLock,
		} );

		UndoSystem.Add( creator: this.Owner, callback: () =>
		{
			if ( go.IsValid() )
			{
				ClearPhysicsLocking( go );
				return "Removed physics locking";
			}
			return "";
		}, prop: go );

		return true;
	}

	[Rpc.Broadcast]
	private static void SetPhysicsLocking( GameObject gameObject, PhysicsLock physicsLock )
	{
		var rigidBody = gameObject?.GetComponent<Rigidbody>();
		if ( !rigidBody.IsValid() )
			return;

		rigidBody.Locking = physicsLock;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "reload" ) )
			return false;
		if ( !trace.Hit )
			return false;

		ClearPhysicsLocking( trace.GameObject );

		return true;
	}

	[Rpc.Broadcast]
	private static void ClearPhysicsLocking( GameObject gameObject )
	{
		var rigidBody = gameObject?.GetComponent<Rigidbody>();
		if ( !rigidBody.IsValid() )
			return;

		rigidBody.Locking = new PhysicsLock();
	}
}
