public partial class PhysGun : BaseWeapon, Component.INetworkListener
{
	[Property] public float MinTargetDistance { get; set; } = 0.0f;
	[Property] public float MaxTargetDistance { get; set; } = 10000.0f;
	[Property] public float LinearFrequency { get; set; } = 20.0f;
	[Property] public float LinearDampingRatio { get; set; } = 1.0f;
	[Property] public float AngularFrequency { get; set; } = 20.0f;
	[Property] public float AngularDampingRatio { get; set; } = 1.0f;
	[Property] public float TargetDistanceSpeed { get; set; } = 25.0f;
	[Property] public float RotateSpeed { get; set; } = 0.2f;
	[Property] public float RotateSnapAt { get; set; } = 45.0f;

	[Sync] public bool Beaming { get; set; }
	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync] public GameObject GrabbedObject { get; set; }
	[Sync] public Vector3 GrabbedPos { get; set; }
	[Sync] public int GrabbedBone { get; set; } = -1;

	private SoundHandle BeamSound;
	private bool BeamSoundPlaying;
	GameObject lastGrabbed = null;
	private Model _model = Cloud.Model("katka/gravitygun"); // in the prefab, but this ensures its downloaded

	PhysicsBody _heldBody;
	PhysicsBody HeldBody
	{
		get
		{
			if ( GrabbedObject != lastGrabbed && GrabbedObject != null )
			{
				_heldBody = GetBody( GrabbedObject, GrabbedBone );
			}

			lastGrabbed = GrabbedObject;
			return _heldBody;
		}
	}

	PhysicsBody GetBody( GameObject gameObject, int bone )
	{
		if ( bone > -1 )
		{
			ModelPhysics modelPhysics = gameObject.Components.Get<ModelPhysics>();
			return modelPhysics.IsValid() ? modelPhysics.PhysicsGroup.GetBody( bone ) : null;
		}
		else
		{
			Rigidbody rigidbody = gameObject.Components.Get<Rigidbody>();
			return rigidbody.IsValid() ? rigidbody.PhysicsBody : null;
		}
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		GrabbedObject = null;
		SetRendererAnimParam( "deploy", true );
		SetRendererAnimParam( "moveback", 1 );
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		TryEndGrab();
		StopBeamSound();
	}

	protected override void OnPreRender()
	{
		UpdateEffects();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !IsProxy )
		{
			ViewModel?.Renderer.SceneObject.Attributes.Set( "colortint", Color.Cyan );
			ProngsState = ProngsState.LerpTo( grabbed ? 1 : 0, Time.Delta * 10f );
			SetRendererAnimParam( "prongs", ProngsState );
		}

		if ( Owner.IsValid() && Owner.Controller.IsValid() )
		{
			Owner.Controller.EnablePressing = !GrabbedObject.IsValid();
			Owner.Controller.UseInputControls = !Input.Down( "use" ) || !GrabbedObject.IsValid();

			if ( !Owner.Controller.UseInputControls )
				Owner.Controller.WishVelocity = 0;
		}

		if ( !GrabbedObject.IsValid() )
			return;

		if ( !HeldBody.IsValid() )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.075f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.075f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	private float ProngsState { get; set; } = 0;
	bool grabbed;

	public override void OnControl()
	{
		Beaming = Input.Down( "attack1" );

		if ( Input.Pressed( "attack1" ) )
			BroadcastAttack();

		if ( !GrabbedObject.IsValid() && Beaming && !grabbed && TryStartGrab() )
		{
			grabbed = true;
			SetRendererAnimParam( "hold", true );
		}

		if ( Beaming && !GrabbedObject.IsValid() && !Grab().isValid )
			grabbed = false;

		if ( Input.Released( "attack1" ) )
		{
			TryEndGrab();
			SetRendererAnimParam( "drop", true );

			grabbed = false;
		}

		if ( Input.Pressed( "reload" ) && Input.Down( "run" ) )
		{
			TryUnfreezeAll();
			SetRendererAnimParam( "fire", true );
		}

		if ( Beaming )
		{
			if ( !(BeamSound?.IsPlaying ?? false) && !BeamSoundPlaying && Owner.Settings.UsePhysgunSound )
			{
				BeamSound = GameObject.PlaySound( ResourceLibrary.Get<SoundEvent>( "sounds/weapons/gravity_gun/superphys_small_zap1.sound" ) );
				BeamSound.Volume = 0.10f;
				BeamSoundPlaying = true;
			}
		}
		else
		{
			StopBeamSound();
		}

		if ( !GrabbedObject.IsValid() )
			return;

		if ( Input.Pressed( "attack2" ) )
		{
			BroadcastAttack();
			Freeze( GrabbedObject, GrabbedBone );
			SetRendererAnimParam( "fire", true );

			TryEndGrab();
			return;
		}

		MoveTargetDistance( Input.MouseWheel.y * TargetDistanceSpeed );

		if ( Input.Down( "use" ) )
			DoRotate( new Angles( 0.0f, Owner.EyeTransform.Rotation.Angles().yaw, 0.0f ), Input.MouseDelta * RotateSpeed );

		HoldPos = Owner.AimRay.Position - heldPos * HeldBody.Rotation + Owner.AimRay.Forward * holdDistance;
		HoldRot = Owner.Controller.EyeAngles * heldRot;

		if ( Input.Down( "run" ) && Input.Down( "use" ) )
		{
			var angles = HoldRot.Angles();

			HoldRot = Rotation.From(
				MathF.Round( angles.pitch / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.yaw / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.roll / RotateSnapAt ) * RotateSnapAt
			);
		}
	}

	[Rpc.Broadcast]
	private void TryUnfreezeAll()
	{
		var rootEnt = GrabbedObject;

		if ( !GrabbedObject.IsValid() )
		{
			var tr = Scene.Trace.Ray( Owner.AimRay, MaxTargetDistance )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

			if ( !tr.Hit || !tr.GameObject.IsValid() || tr.Component is MapCollider ) return;

			rootEnt = tr.GameObject.Root;
		}

		if ( !rootEnt.IsValid() )
			return;

		var weldContexts = GetAllConnectedProps( rootEnt );

		for ( int i = 0; i < weldContexts.Count; i++ )
		{
			ModelPhysics modelPhysics = weldContexts[i]?.GetComponent<ModelPhysics>();
			Rigidbody rigidbody = weldContexts[i]?.GetComponent<Rigidbody>();

			var body = modelPhysics.IsValid() ? modelPhysics?.PhysicsGroup?.GetBody( 0 ) : (rigidbody.IsValid() ? weldContexts[i]?.GetComponent<Rigidbody>()?.PhysicsBody : null);

			if ( !body.IsValid() ) continue;

			if ( body.PhysicsGroup.IsValid() )
			{
				foreach ( var b in body.PhysicsGroup.Bodies )
				{
					if ( b.BodyType == PhysicsBodyType.Static )
					{
						b.BodyType = PhysicsBodyType.Dynamic;
					}
				}
			}
			else if ( body.BodyType == PhysicsBodyType.Static )
			{
				body.BodyType = PhysicsBodyType.Dynamic;
			}
		}
	}

	public static List<GameObject> GetAllConnectedProps( GameObject gameObject )
	{
		PropHelper propHelper = gameObject.Components.Get<PropHelper>();

		if ( !propHelper.IsValid() )
			return null;

		var result = new List<Joint>();
		var visited = new HashSet<PropHelper>();

		CollectWelds( propHelper, result, visited );

		List<GameObject> returned = new List<GameObject> { gameObject };

		foreach ( Joint joint in result )
		{
			GameObject object1 = joint?.Body1?.GetGameObject();
			GameObject object2 = joint?.Body2?.GetGameObject();

			if ( object1.IsValid() && !returned.Contains( object1 ) ) returned.Add( object1 );
			if ( object2.IsValid() && !returned.Contains( object2 ) ) returned.Add( object2 );
		}

		return returned;
	}

	private static void CollectWelds( PropHelper propHelper, List<Joint> result, HashSet<PropHelper> visited )
	{
		if ( visited.Contains( propHelper ) )
			return;

		visited.Add( propHelper );
		result.AddRange( propHelper.Joints );

		foreach ( var joint in propHelper.Joints )
		{
			GameObject jointObject = joint.Body1?.GetGameObject();

			if ( jointObject == propHelper.GameObject )
			{
				jointObject = joint.Body2?.GetGameObject();
			}

			if ( !jointObject.IsValid() )
				return;

			PropHelper propHelper1 = jointObject.Components.GetInParentOrSelf<PropHelper>();

			if ( !propHelper1.IsValid() )
				return;

			CollectWelds( propHelper1, result, visited );
		}
	}

	Vector3 heldPos;
	Rotation heldRot;
	float heldMass;
	float holdDistance;

	private bool TryStartGrab()
	{
		(var isValid, var tr) = Grab();

		if ( !isValid )
			return false;

		if ( !tr.Body.IsValid() )
			return false;

		var rootEnt = tr.GameObject;
		GrabbedObject = rootEnt;

		bool isRagdoll = GrabbedObject.Components.Get<ModelPhysics>().IsValid();
		GrabbedBone = isRagdoll ? tr.Body.GroupIndex : -1;

		if ( !HeldBody.IsValid() )
			return false;

		holdDistance = Vector3.DistanceBetween( Owner.AimRay.Position, tr.EndPosition );
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );

		heldRot = Owner.Controller.EyeAngles.ToRotation().Inverse * HeldBody.Rotation;
		heldPos = HeldBody.Transform.PointToLocal( tr.EndPosition );
		heldMass = HeldBody.Mass;
		if ( !isRagdoll )
		{
			HeldBody.Mass = 10000f;
		}

		HoldPos = HeldBody.Position;
		HoldRot = HeldBody.Rotation;

		GrabbedPos = tr.Body.Transform.PointToLocal( tr.EndPosition );

		UnFreeze( GrabbedObject, GrabbedBone );

		return true;
	}

	(bool isValid, SceneTraceResult result) Grab()
	{
		var tr = Scene.Trace.Ray( Owner.AimRay, MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "debris", "nocollide" )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

		bool valid =
			tr.Hit &&
			(tr.GameObject.Components.Get<Rigidbody>().IsValid() || tr.GameObject.Components.Get<ModelPhysics>().IsValid()) &&
			tr.GameObject.IsValid() &&
			tr.Component is not MapCollider &&
			!tr.StartedSolid &&
			!tr.Tags.Contains( "grabbed" );

		return (valid, tr);
	}

	[Rpc.Broadcast]
	private void BroadcastAttack()
	{
		Owner?.Controller?.Renderer?.Set( "b_attack", true );
	}

	[Rpc.Broadcast]
	private void TryEndGrab()
	{
		if ( GrabbedObject.IsValid() && HeldBody.IsValid() )
		{
			HeldBody.Mass = heldMass;
		}
		GrabbedObject = null;
		lastGrabbed = null;
	}

	protected void StopBeamSound()
	{
		BeamSound?.Stop();
		BeamSoundPlaying = false;
	}

	private void MoveTargetDistance( float distance )
	{
		holdDistance += distance;
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
	}

	public void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x * RotateSpeed );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y * RotateSpeed );
		localRot = eye.Inverse * localRot;

		heldRot = localRot * heldRot;
	}

	[Rpc.Broadcast]
	public void Freeze( GameObject gameObject, int bone )
	{
		if ( !gameObject.IsValid() )
			return;

		PhysicsBody body = GetBody( gameObject, bone );
		if ( body.IsValid() )
			body.BodyType = PhysicsBodyType.Static;

		FreezeEffects();
	}

	[Rpc.Broadcast]
	public void UnFreeze( GameObject gameObject, int bone )
	{
		if ( !gameObject.IsValid() )
			return;

		PhysicsBody body = GetBody( gameObject, bone );
		if ( body.IsValid() )
			body.BodyType = PhysicsBodyType.Dynamic;
	}
}
