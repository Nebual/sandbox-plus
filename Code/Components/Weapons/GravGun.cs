public partial class GravGun : BaseWeapon, IPlayerEvent
{
	[Property] public float MaxPullDistance => 2000.0f;
	[Property] public float MaxPushDistance => 500.0f;
	[Property] public float LinearFrequency => 10.0f;
	[Property] public float LinearDampingRatio => 1.0f;
	[Property] public float AngularFrequency => 10.0f;
	[Property] public float AngularDampingRatio => 1.0f;
	[Property] public float PullForce => 20.0f;
	[Property] public float PushForce => 1000.0f;
	[Property] public float ThrowForce => 2000.0f;
	[Property] public float HoldDistance => 50.0f;
	[Property] public float MaxHoldDistanceBeforeDrop = 100f;
	[Property] public float AttachDistance => 150.0f;
	[Property] public float DropCooldown => 0.5f;
	[Property] public float BreakLinearForce => 2000.0f;

	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync, Property] public GameObject GrabbedObject { get; set; }
	[Sync] public Vector3 GrabbedPos { get; set; }
	[Sync] public int GrabbedBone { get; set; } = -1;

	GameObject lastGrabbed = null;

	private bool prongsActive = false;
	private float ProngsState { get; set; } = 0;
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
			return modelPhysics.PhysicsGroup.GetBody( bone );
		}
		else
		{
			Rigidbody rigidbody = gameObject.Components.Get<Rigidbody>();
			return rigidbody.PhysicsBody;
		}
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		GrabbedObject = null;
		ViewModel?.Renderer.Set( "deploy", true );
		ViewModel?.Renderer.Set( "moveback", 1 );
	}

	protected override void OnUpdate()
	{
		Move();
		base.OnUpdate();
		if ( !IsProxy )
		{
			ProngsState = ProngsState.LerpTo( prongsActive ? 1 : 0, Time.Delta * 10f );
			ViewModel?.Renderer.Set( "prongs", ProngsState );
		}
		ViewModel?.Renderer.SceneObject.Attributes.Set( "colortint", Color.FromBytes( 172, 64, 0 ) );
	}

	TimeSince timeSinceImpulse;

	void Move()
	{
		if ( !GrabbedObject.IsValid() )
			return;

		if ( timeSinceImpulse < Time.Delta * 5 )
			return;

		if ( !HeldBody.IsValid() )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.1f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.1f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	TimeSince timeSinceDrop;

	public override void OnControl()
	{
		Owner.Controller.EnablePressing = !GrabbedObject.IsValid();

		var eyePos = Owner.Controller.EyePosition;
		var eyeRot = Owner.Controller.EyeAngles;
		var eyeDir = Owner.Controller.EyeAngles.Forward;

		if ( HeldBody.IsValid() )
		{
			if ( Input.Pressed( "attack1" ) )
			{
				GameObject grabbedObject = GrabbedObject;
				int grabbedBone = GrabbedBone;
				PhysicsBody heldBody = HeldBody;

				GrabEnd();

				if ( !grabbedObject.IsValid() || !heldBody.IsValid() )
					return;

				if ( heldBody.PhysicsGroup.IsValid() && heldBody.PhysicsGroup.BodyCount > 1 )
				{
					for ( int i = 0; i < heldBody.PhysicsGroup.Bodies.Count(); i++ )
					{
						ApplyImpulse( grabbedObject, i, eyeDir * (heldBody.Mass * ThrowForce * 0.5f) );
						ApplyAngularImpulse( grabbedObject, i, heldBody.Mass * Vector3.Random * ThrowForce );
					}
				}
				else
				{
					ApplyImpulse( grabbedObject, grabbedBone, eyeDir * (heldBody.Mass * ThrowForce) );
					ApplyAngularImpulse( grabbedObject, grabbedBone, Vector3.Random * (heldBody.Mass * ThrowForce) );
				}
				ViewModel?.Renderer.Set( "altfire", true );
			}
			else if ( Input.Pressed( "attack2" ) )
			{
				GrabEnd();
				ViewModel?.Renderer.Set( "drop", true );
			}
			else
			{
				GrabMove( eyePos, eyeDir, eyeRot );
			}
			prongsActive = true;

			return;
		}

		if ( timeSinceDrop < DropCooldown )
			return;
		prongsActive = false;

		var tr = Scene.Trace.Ray( eyePos, eyePos + eyeDir * MaxPullDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris", "nocollide" )
			.IgnoreGameObjectHierarchy( Owner.GameObject )
			.Radius( 2.0f )
			.Run();

		if ( !tr.Hit || !tr.GameObject.IsValid() || !tr.Body.IsValid() || !tr.GameObject.IsValid() || tr.Component is MapCollider )
			return;

		var rigidBody = tr.GameObject.Components.Get<Rigidbody>();
		var modelPhysics = tr.GameObject.Components.Get<ModelPhysics>();

		if ( !rigidBody.IsValid() && !modelPhysics.IsValid() )
			return;

		if ( tr.GameObject.Tags.Has( "grabbed" ) )
			return;

		var body = tr.Body;

		if ( !tr.Body.IsValid() )
			return;

		if ( body.BodyType != PhysicsBodyType.Dynamic )
			return;

		if ( eyePos.Distance( body.MassCenter ) < AttachDistance )
			prongsActive = true;

		if ( Input.Pressed( "attack1" ) )
		{
			if ( tr.Distance < MaxPushDistance )
			{
				var pushScale = 1.0f - Math.Clamp( tr.Distance / MaxPushDistance, 0.0f, 1.0f );
				ApplyImpulseAt( tr.GameObject, modelPhysics.IsValid() ? body.GroupIndex : -1, tr.EndPosition, eyeDir * (body.Mass * (PushForce * pushScale)) );
			}
			ViewModel?.Renderer.Set( "fire", true );
		}
		else if ( Input.Down( "attack2" ) )
		{
			var attachPos = body.FindClosestPoint( eyePos );

			if ( eyePos.Distance( attachPos ) <= AttachDistance )
			{
				var holdDistance = HoldDistance + attachPos.Distance( body.MassCenter );

				GrabStart( tr.GameObject, body, eyePos + eyeDir * holdDistance, eyeRot );
				ViewModel?.Renderer.Set( "hold", true );
			}
			else
			{
				if ( tr.Body.PhysicsGroup.IsValid() )
				{
					for ( int i = 0; i < tr.Body.PhysicsGroup.Bodies.Count(); i++ )
					{
						ApplyImpulse( tr.GameObject, i, eyeDir * -PullForce * tr.Body.PhysicsGroup.Bodies.ElementAt( i ).Mass );
					}
				}
				else
				{
					ApplyImpulse( tr.GameObject, -1, eyeDir * -PullForce * tr.Body.Mass );
				}
			}
		}
	}

	[Rpc.Broadcast]
	private void ApplyImpulseAt( GameObject gameObject, int bodyIndex, Vector3 position, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		PhysicsBody body = null;

		if ( bodyIndex > -1 && gameObject.Components.TryGet<ModelPhysics>( out var modelPhysics ) )
		{
			body = modelPhysics.PhysicsGroup.Bodies.ElementAt( bodyIndex );
		}
		else if ( gameObject.Components.TryGet<Rigidbody>( out var rigidbody ) )
		{
			body = rigidbody.PhysicsBody;
		}

		if ( !body.IsValid() )
			return;

		body.ApplyImpulseAt( position, velocity );
	}

	[Rpc.Broadcast]
	private void ApplyImpulse( GameObject gameObject, int bodyIndex, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		PhysicsBody body = null;

		if ( bodyIndex > -1 && gameObject.Components.TryGet<ModelPhysics>( out var modelPhysics ) )
		{
			body = modelPhysics.PhysicsGroup.GetBody( bodyIndex );
		}
		else if ( gameObject.Components.TryGet<Rigidbody>( out var rigidbody ) )
		{
			body = rigidbody.PhysicsBody;
		}

		if ( !body.IsValid() )
			return;

		if ( body.IsValid() ) body.ApplyImpulse( velocity );
	}

	[Rpc.Broadcast]
	private void ApplyAngularImpulse( GameObject gameObject, int bodyIndex, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		PhysicsBody body = null;

		if ( bodyIndex > -1 )
		{
			var modelPhysics = gameObject.Components.Get<ModelPhysics>();
			if ( modelPhysics.IsValid() && modelPhysics.PhysicsGroup.IsValid() && bodyIndex < modelPhysics.PhysicsGroup.Bodies.Count() )
			{
				body = modelPhysics.PhysicsGroup.GetBody( bodyIndex );
			}
		}
		else
		{
			body = gameObject.Components.Get<Rigidbody>()?.PhysicsBody;
		}

		if ( body.IsValid() ) body.ApplyAngularImpulse( velocity );
	}

	Vector3 heldPos;
	Rotation heldRot;

	private void GrabStart( GameObject gameObject, PhysicsBody body, Vector3 grabPos, Rotation grabRot )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd();

		GrabbedObject = gameObject;

		bool isRagdoll = GrabbedObject.Components.Get<ModelPhysics>().IsValid();
		GrabbedBone = isRagdoll ? body.GroupIndex : -1;

		if ( !HeldBody.IsValid() )
			return;

		heldRot = Owner.Controller.EyeAngles.ToRotation().Inverse * HeldBody.Rotation;
		heldPos = HeldBody.LocalMassCenter;

		HoldPos = HeldBody.Position;
		HoldRot = HeldBody.Rotation;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot )
	{
		if ( !HeldBody.IsValid() )
			return;

		var attachPos = HeldBody.FindClosestPoint( startPos );

		if ( startPos.Distance( attachPos ) > MaxHoldDistanceBeforeDrop )
		{
			GrabEnd();
			ViewModel?.Renderer.Set( "drop", true );
			return;
		}

		var holdDistance = HoldDistance + attachPos.Distance( HeldBody.MassCenter );

		HoldPos = startPos - heldPos * HeldBody.Rotation + dir * holdDistance;
		HoldRot = rot * heldRot;
	}

	[Rpc.Broadcast]
	private void GrabEnd()
	{
		timeSinceDrop = 0;
		heldRot = Rotation.Identity;

		if ( GrabbedObject.IsValid() )
		{
			GrabbedObject.Tags.Remove( "grabbed" );
		}

		GrabbedObject = null;
		lastGrabbed = null;
		_heldBody = null;
	}
}
