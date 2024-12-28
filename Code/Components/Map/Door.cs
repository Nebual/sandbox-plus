public sealed class Door : Component, Component.IPressable
{
	/// <summary>
	/// Animation curve to use, X is the time between 0-1 and Y is how much the door is open to its target angle from 0-1.
	/// </summary>
	[Property] public Curve AnimationCurve { get; set; } = new Curve( new Curve.Frame( 0f, 0f ), new Curve.Frame( 1f, 1.0f ) );

	/// <summary>
	/// Sound to play when a door is opened.
	/// </summary>
	[Property, Group( "Sound" )] public SoundEvent OpenSound { get; set; }

	/// <summary>
	/// Sound to play when a door is fully opened.
	/// </summary>
	[Property, Group( "Sound" )] public SoundEvent OpenFinishedSound { get; set; }

	/// <summary>
	/// Sound to play when a door is closed.
	/// </summary>
	[Property, Group( "Sound" )] public SoundEvent CloseSound { get; set; }

	/// <summary>
	/// Sound to play when a door has finished closing.
	/// </summary>
	[Property, Group( "Sound" )] public SoundEvent CloseFinishedSound { get; set; }

	/// <summary>
	/// Optional pivot point, origin will be used if not specified.
	/// </summary>
	[Property] public GameObject Pivot { get; set; }

	/// <summary>
	/// The axis the door rotates/moves on.
	/// </summary>
	[Property] public Angles Axis { get; set; } = new Angles( 90.0f, 0, 0 );

	/// <summary>
	/// How far should the door rotate.
	/// </summary>
	[Property] public float Distance { get; set; } = 90.0f;

	/// <summary>
	/// How long in seconds should it take to open this door.
	/// </summary>
	[Property] public float OpenTime { get; set; } = 0.5f;

	/// <summary>
	/// Open away from the person who uses this door.
	/// </summary>
	[Property] public bool OpenAwayFromPlayer { get; set; } = true;

	/// <summary>
	/// Can we open the door at all?
	/// </summary>
	[Property] public bool Locked { get; set; } = false;

	[Property] public ModelCollider Collider;

	public enum DoorMoveType
	{
		Moving,
		Rotating,
		AnimatingOnly
	}

	[Property] public DoorMoveType MoveDirType { get; set; } = DoorMoveType.Rotating;

	public enum DoorState
	{
		Open,
		Opening,
		Closing,
		Closed
	}

	Transform StartTransform { get; set; }
	public Vector3 PivotPosition { get; set; }
	bool ReverseDirection { get; set; }

	[Sync(SyncFlags.FromHost)] public TimeSince LastUse { get; set; }
	[Sync(SyncFlags.FromHost)] public DoorState State { get; set; } = DoorState.Closed;

	private DoorState DefaultState { get; set; } = DoorState.Closed;

	private string _hintText = "open";
	public string HintText
	{
		get => _hintText;
		set => _hintText = value;
	}

	protected override void OnStart()
	{
		StartTransform = Transform.Local;

		if ( PivotPosition == Vector3.Zero )
			PivotPosition = Pivot is not null ? Pivot.WorldPosition : StartTransform.Position;

		DefaultState = State;
	}

	public bool CanUse()
	{
		// Don't use doors already opening/closing
		return State is DoorState.Open or DoorState.Closed;
	}

	bool IPressable.CanPress( IPressable.Event e )
	{
		return CanUse();
	}

	private void PlaySound( SoundEvent resource )
	{
		PlaySoundRpc( resource.ResourceId );
	}

	[Rpc.Broadcast]
	private void PlaySoundRpc( int resourceId )
	{
		var resource = ResourceLibrary.Get<SoundEvent>( resourceId );
		if ( resource == null ) return;

		var handle = Sound.Play( resource, WorldPosition );
		if ( !handle.IsValid() ) return;

		handle.Occlusion = false;
	}

	[Rpc.Broadcast]
	public void Press( GameObject presser )
	{
		if ( presser.Network.Owner != Rpc.Caller )
			return;

		LastUse = 0.0f;

		if ( Locked )
			return;

		if ( State == DoorState.Closed )
		{
			if ( OpenAwayFromPlayer )
			{
				var doorToPlayer = (presser.WorldPosition - PivotPosition).Normal;
				var doorForward = Transform.Local.Rotation.Forward;

				ReverseDirection = Vector3.Dot( doorToPlayer, doorForward ) > 0;
			}

			Open();
		}
		else if ( State == DoorState.Open )
		{
			Close();
		}
	}

	bool IPressable.Press( IPressable.Event e )
	{
		if ( State == DoorState.Opening || State == DoorState.Closing )
			return false;

		Press( e.Source.GameObject );
		return true;
	}

	public void Toggle()
	{
		if ( State == DoorState.Closed )
		{
			Open();
		}
		else if ( State == DoorState.Open )
		{
			Close();
		}
	}
	public void Open()
	{
		State = DoorState.Opening;

		if ( OpenSound is not null )
			PlaySound( OpenSound );
	}

	public void Close()
	{
		State = DoorState.Closing;

		if ( CloseSound is not null )
			PlaySound( CloseSound );
	}

	protected override void OnFixedUpdate()
	{
		if ( State != DoorState.Opening && State != DoorState.Closing )
			return;

		var time = LastUse.Relative.Remap( 0.0f, OpenTime, 0.0f, 1.0f );
		var curve = AnimationCurve.Evaluate( time );

		if ( State == DoorState.Closing ) curve = 1.0f - curve;

		if ( MoveDirType == DoorMoveType.Rotating )
		{
			var targetAngle = Distance;
			if ( ReverseDirection ) targetAngle *= -1.0f;

			var axis = Rotation.From( Axis ).Up;

			Transform.Local = StartTransform.RotateAround( PivotPosition, Rotation.FromAxis( axis, targetAngle * curve ) );
		}

		if ( MoveDirType == DoorMoveType.Moving )
		{
			var dir = Axis.Forward;
			var boundSize = Collider.KeyframeBody.GetBounds().Size;
			var fulldirection = dir * (MathF.Abs( boundSize.Dot( dir ) ) - Distance);

			Transform.Local = StartTransform.WithPosition( StartTransform.Position + (fulldirection * curve) );
		}

		// If we're done finalize the state and play the sound
		if ( time < 1f ) return;

		State = State == DoorState.Opening ? DoorState.Open : DoorState.Closed;

		if ( !Networking.IsClient )
		{
			if ( State == DoorState.Open && OpenFinishedSound is not null )
				PlaySound( OpenFinishedSound );

			if ( State == DoorState.Closed && CloseFinishedSound is not null )
				PlaySound( CloseFinishedSound );
		}
	}
}
