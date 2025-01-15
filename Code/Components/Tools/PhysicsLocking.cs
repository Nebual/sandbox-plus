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

	public override Dictionary<string, Action> GetPresetOptions()
	{
		return (new() {
			{ "Keep Upright", () => PhysicsLocking.SetConfig(PitchLock: true, RollLock: true) },
			{ "Elevator", () => PhysicsLocking.SetConfig(PitchLock: true, YawLock: true, RollLock: true, XLock: true, YLock: true) },
			{ "Prevent Rotation", () => PhysicsLocking.SetConfig(PitchLock: true, YawLock: true, RollLock: true) },
			{ "Prevent Movement", () => PhysicsLocking.SetConfig(XLock: true, YLock: true, ZLock: true) },
			{ "Prevent Rotation & Movement", () => PhysicsLocking.SetConfig(PitchLock: true, YawLock: true, RollLock: true, XLock: true, YLock: true, ZLock: true) },
		});
	}
	public static void SetConfig( bool PitchLock = false, bool YawLock = false, bool RollLock = false, bool XLock = false, bool YLock = false, bool ZLock = false )
	{
		if ( Instance is PhysicsLocking self )
		{
			self.PitchLock = PitchLock;
			self.YawLock = YawLock;
			self.RollLock = RollLock;
			self.XLock = XLock;
			self.YLock = YLock;
			self.ZLock = ZLock;
		}
	}

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
