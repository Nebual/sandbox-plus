using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

[Spawnable]
[Library( "physgun" )]
public partial class PhysGun : Carriable
{
	private static int ViewModelIndex = 0;
	public override string ViewModelPath => ViewModelIndex == 0 ? Cloud.Asset( "katka/gravitygun" ) : Cloud.Asset( "wiremod.v_gravity_gun2" );
	private AnimatedEntity ViewModelArms { get; set; }
	private AnimatedEntity ArmsAdapter { get; set; }
	public List<CapsuleLightEntity> LightsWorld;
	public PointLightEntity LightView;
	public Color CrystalColor { get; set; } = Color.Cyan;

	public PhysicsBody HeldBody { get; private set; }
	public Vector3 HeldPos { get; private set; }
	public Rotation HeldRot { get; private set; }
	public float HeldMass { get; private set; }
	public Vector3 HoldPos { get; private set; }
	public Rotation HoldRot { get; private set; }
	public float HoldDistance { get; private set; }
	public bool Grabbing { get; private set; }

	protected virtual float MinTargetDistance => 0.0f;
	protected virtual float MaxTargetDistance => 10000.0f;
	protected virtual float LinearFrequency => 20.0f;
	protected virtual float LinearDampingRatio => 1.0f;
	protected virtual float AngularFrequency => 20.0f;
	protected virtual float AngularDampingRatio => 1.0f;
	protected virtual float TargetDistanceSpeed => 25.0f;
	protected virtual float RotateSpeed => 0.25f;
	protected virtual float RotateSnapAt => 45.0f;

	public const string GrabbedTag = "grabbed";
	public const string PhysgunBlockTag = "physgun-block"; // will be hit by a Physgun ray, and then stopped

	[Net] public bool BeamActive { get; set; }
	[Net] public Entity GrabbedEntity { get; set; }
	[Net] public int GrabbedBone { get; set; }
	[Net] public Vector3 GrabbedPos { get; set; }

	private Sound BeamSound;
	private bool BeamSoundPlaying;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ViewModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		Tags.Add( "weapon", "solid" );
	}

	[ConCmd.Client( "physgun_model" )]
	public static void ChangeModelCmd( string cmd )
	{
		ViewModelIndex = int.Parse( cmd ) % 2;
		var gun = (Game.LocalPawn as SandboxPlayer).ActiveChild;
		if ( gun.IsValid() && gun is PhysGun physGun )
		{
			physGun.ActiveEnd( physGun, false );
			physGun.ActiveStart( physGun );
		}
	}

	public void CreateLights()
	{
		LightsWorld = new();

		for ( var i = 1; i <= 6; i++ )
		{
			var light = new CapsuleLightEntity();
			light.CapsuleLength = 5.75f;
			light.LightSize = 0.25f;
			light.Brightness = 0.0005f;
			light.Enabled = false;
			LightsWorld.Add( light );
		}

		LightView = new PointLightEntity();
		LightView.LightSize = 0.25f;
		LightView.Brightness = 0.0005f;
		LightView.Enabled = false;
	}

	public void DestroyLights()
	{
		if ( LightsWorld != null )
		{
			foreach ( var light in LightsWorld )
			{
				light.Delete();
			}
			LightsWorld.Clear();
		}

		LightView?.Delete();
	}

	public override void CreateViewModel()
	{
		base.CreateViewModel();

		ViewModelEntity.EnableViewmodelRendering = true;
		if ( ViewModelIndex == 0 )
		{
			ViewModelEntity.SetBodyGroup( "crystal_inside", 1 );

			ArmsAdapter = new AnimatedEntity( Cloud.Asset( "katka/hand_adapter_valvebiped_to_sbox" ) );
			ArmsAdapter.SetParent( ViewModelEntity, true );
			ArmsAdapter.EnableViewmodelRendering = ViewModelEntity.EnableViewmodelRendering;

			ViewModelArms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
			ViewModelArms.SetParent( ArmsAdapter, true );
			ViewModelArms.EnableViewmodelRendering = ViewModelEntity.EnableViewmodelRendering;
		}
		else if ( ViewModelIndex == 1 )
		{
			ViewModelEntity.SetMaterialGroup( "physicsgun" );
		}
	}

	public override void DestroyViewModel()
	{
		base.DestroyViewModel();

		ViewModelArms?.Delete();
		ArmsAdapter?.Delete();
	}

	[GameEvent.Client.Frame]
	public void ProcessLights()
	{
		if ( !this.IsValid() ) return;

		SceneObject?.Attributes.Set( "colortint", CrystalColor );
		ViewModelEntity?.SceneObject?.Attributes.Set( "colortint", CrystalColor );

		if ( LightsWorld != null )
		{
			for ( var i = 1; i <= 6; i++ )
			{
				var t = (Transform)GetAttachment( $"glow{i}" );
				if ( LightsWorld.Count >= i )
				{
					var light = LightsWorld.ElementAt( i - 1 );

					if ( !light.IsValid() ) continue;

					light.Color = CrystalColor;
					light.Position = t.Position;
					light.Rotation = t.Rotation;
					light.Enabled = !IsFirstPersonMode && ViewModelEntity.IsValid();
				}
			}
		}

		if ( LightView.IsValid() )
		{
			if ( ViewModelEntity.IsValid() )
			{
				var m = (Transform)ViewModelEntity.GetAttachment( "muzzle" );
				LightView.Color = CrystalColor;
				LightView.Position = m.Position;
				LightView.Rotation = m.Rotation;
				LightView.Enabled = IsFirstPersonMode && ViewModelEntity.IsValid();
				LightView.LightSize = 0.025f;
				LightView.Brightness = 0.01f;
			}
		}
	}

	[GameEvent.Entity.PreCleanup]
	protected void OnEntityPreCleanup()
	{
		GrabEnd();
	}

	[ClientRpc]
	public void SetViewModelParam( string param, bool value = true )
	{
		ViewModelEntity?.SetAnimParameter( param, value );
	}

	public override void Simulate( IClient client )
	{
		if ( Owner is not Player owner ) return;

		ViewModelEntity?.SetAnimParameter( "moveback", 0.85f );

		var eyePos = owner.EyePosition;
		var eyeDir = owner.EyeRotation.Forward;
		var eyeRot = Rotation.From( new Angles( 0.0f, owner.EyeRotation.Yaw(), 0.0f ) );

		if ( Input.Pressed( "attack1" ) )
		{
			//(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

			//ViewModelEntity?.SetAnimParameter( "fire", true );

			if ( !Grabbing )
				Grabbing = true;
		}

		bool grabEnabled = Grabbing && Input.Down( "attack1" );
		bool wantsToFreeze = Input.Pressed( "attack2" );

		if ( GrabbedEntity.IsValid() && wantsToFreeze )
		{
			//(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
			SetViewModelParam( To.Single( Owner ), "fire" );
		}

		BeamActive = grabEnabled;

		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{
				if ( grabEnabled )
				{
					if ( HeldBody.IsValid() )
					{
						UpdateGrab( eyePos, eyeRot, eyeDir, wantsToFreeze );
					}
					else
					{
						TryStartGrab( eyePos, eyeRot, eyeDir );
					}
				}
				else if ( Grabbing )
				{
					GrabEnd();

					SetViewModelParam( To.Single( owner ), "drop" );
				}

				if ( !Grabbing && Input.Pressed( "reload" ) )
				{
					TryUnfreezeAll( eyePos, eyeRot, eyeDir );
				}
			}
		}

		if ( Game.IsClient )
		{
			if ( BeamActive )
			{
				Input.MouseWheel = 0;

				if ( !BeamSound.IsPlaying && !BeamSoundPlaying )
				{
					BeamSound = PlaySound( "sounds/weapons/gravity_gun/superphys_small_zap1.sound" );
					BeamSoundPlaying = true;
				}
			}
			else
			{
				StopBeamSound();
			}

			if ( Input.Pressed( "drop" ) && Input.Down( "run" ) )
			{
				ChangeModelCmd( ((ViewModelIndex + 1) % 2).ToString() );
			}
		}
	}

	private void TryUnfreezeAll( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "debris", PhysgunBlockTag )
			.Ignore( this )
			.OnTraceEvent( Owner ) // SandboxPlus addition for Stargate support
			.Run();
		tr = CanToolParams.RunCanTool( Owner as Player, ClassName, tr );

		if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld ) return;
		if ( tr.Entity.Tags.Has( PhysgunBlockTag ) ) return;

		var rootEnt = tr.Entity.Root;
		if ( !rootEnt.IsValid() ) return;

		var physicsGroup = rootEnt.PhysicsGroup;
		if ( physicsGroup == null ) return;

		bool unfrozen = false;

		for ( int i = 0; i < physicsGroup.BodyCount; ++i )
		{
			var body = physicsGroup.GetBody( i );
			if ( !body.IsValid() ) continue;

			if ( body.BodyType == PhysicsBodyType.Static )
			{
				body.BodyType = PhysicsBodyType.Dynamic;
				unfrozen = true;
			}
		}

		if ( unfrozen )
		{
			var freezeEffect = Particles.Create( "particles/physgun_freeze.vpcf" );
			freezeEffect.SetPosition( 0, tr.EndPosition );

			SetViewModelParam( To.Single( Owner ), "fire" );
		}
	}

	private void TryStartGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "debris", "nocollide", PhysgunBlockTag )
			.Ignore( this )
			.OnTraceEvent( Owner ) // SandboxPlus addition for Stargate support
			.Run();
		tr = CanToolParams.RunCanTool( Owner as Player, ClassName, tr );

		if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld || tr.StartedSolid ) return;
		if ( tr.Entity.Tags.Has( PhysgunBlockTag ) ) return;

		var rootEnt = tr.Entity.Root;
		var body = tr.Body;

		if ( !body.IsValid() || tr.Entity.Parent.IsValid() )
		{
			if ( rootEnt.IsValid() && rootEnt.PhysicsGroup != null )
			{
				body = (rootEnt.PhysicsGroup.BodyCount > 0 ? rootEnt.PhysicsGroup.GetBody( 0 ) : null);
			}
		}

		if ( !body.IsValid() )
			return;

		//
		// Don't move keyframed, unless it's a player
		//
		if ( body.BodyType == PhysicsBodyType.Keyframed && rootEnt is not Player )
			return;

		//
		// Unfreeze
		//
		if ( body.BodyType == PhysicsBodyType.Static )
		{
			body.BodyType = PhysicsBodyType.Dynamic;
		}

		if ( rootEnt.Tags.Has( GrabbedTag ) )
			return;

		GrabInit( body, eyePos, tr.EndPosition, eyeRot );

		GrabbedEntity = rootEnt;
		GrabbedEntity.Tags.Add( GrabbedTag );
		GrabbedEntity.Tags.Add( $"{GrabbedTag}{Client.SteamId}" );

		GrabbedPos = body.Transform.PointToLocal( tr.EndPosition );
		GrabbedBone = body.GroupIndex;

		Client?.Pvs.Add( GrabbedEntity );
	}

	private void UpdateGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir, bool wantsToFreeze )
	{
		if ( wantsToFreeze )
		{
			if ( HeldBody.BodyType == PhysicsBodyType.Dynamic )
			{
				HeldBody.BodyType = PhysicsBodyType.Static;
			}

			if ( GrabbedEntity.IsValid() )
			{
				var freezeEffect = Particles.Create( "particles/physgun_freeze.vpcf" );
				freezeEffect.SetPosition( 0, HeldBody.Transform.PointToWorld( GrabbedPos ) );
			}

			GrabEnd();
			return;
		}

		MoveTargetDistance( Input.MouseWheel * TargetDistanceSpeed );

		bool rotating = Input.Down( "use" );
		bool snapping = false;

		if ( rotating )
		{
			DoRotate( eyeRot, Input.MouseDelta * RotateSpeed );
			snapping = Input.Down( "run" );
		}

		GrabMove( eyePos, eyeDir, eyeRot, snapping );
	}

	private void Activate()
	{
		if ( !Game.IsServer )
		{
			return;
		}
	}

	private void Deactivate()
	{
		if ( Game.IsServer )
		{
			GrabEnd();
		}

		KillEffects();
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		ViewModelEntity?.SetAnimParameter( "deploy", true );

		Activate();

		if ( Game.IsClient && ViewModelIndex == 0 )
		{
			CreateLights();
		}
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		Deactivate();

		if ( Game.IsClient )
		{
			DestroyLights();
			StopBeamSound();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Deactivate();
		StopBeamSound();
	}

	private void GrabInit( PhysicsBody body, Vector3 startPos, Vector3 grabPos, Rotation rot )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd();

		SetViewModelParam( To.Single( Owner ), "hold" );

		Grabbing = true;
		HeldBody = body;
		HoldDistance = Vector3.DistanceBetween( startPos, grabPos );
		HoldDistance = HoldDistance.Clamp( MinTargetDistance, MaxTargetDistance );

		HeldRot = rot.Inverse * HeldBody.Rotation;
		HeldPos = HeldBody.Transform.PointToLocal( grabPos );

		HoldPos = HeldBody.Position;
		HoldRot = HeldBody.Rotation;

		HeldMass = HeldBody.Mass;
		HeldBody.Mass = 10000f;

		HeldBody.Sleeping = false;
		HeldBody.AutoSleep = false;
	}

	private void GrabEnd()
	{
		if ( HeldBody.IsValid() )
		{
			HeldBody.AutoSleep = true;
			HeldBody.Mass = HeldMass;
		}

		Client?.Pvs.Remove( GrabbedEntity );

		if ( GrabbedEntity.IsValid() )
		{
			GrabbedEntity.Tags.Remove( GrabbedTag );
			GrabbedEntity.Tags.Remove( $"{GrabbedTag}{Client.SteamId}" );
		}

		GrabbedEntity = null;

		HeldBody = null;
		Grabbing = false;
	}

	[GameEvent.Physics.PreStep]
	public void OnPrePhysicsStep()
	{
		if ( !Game.IsServer )
			return;

		if ( !HeldBody.IsValid() )
			return;

		if ( GrabbedEntity is Player )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.075f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.075f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot, bool snapAngles )
	{
		if ( !HeldBody.IsValid() )
			return;

		HoldPos = startPos - HeldPos * HeldBody.Rotation + dir * HoldDistance;

		if ( GrabbedEntity is Player player )
		{
			var velocity = player.Velocity;
			Vector3.SmoothDamp( player.Position, HoldPos, ref velocity, 0.075f, Time.Delta );
			player.Velocity = velocity;
			player.GroundEntity = null;

			return;
		}

		HoldRot = rot * HeldRot;

		if ( snapAngles )
		{
			var angles = HoldRot.Angles();

			HoldRot = Rotation.From(
				MathF.Round( angles.pitch / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.yaw / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.roll / RotateSnapAt ) * RotateSnapAt
			);
		}
	}

	private void MoveTargetDistance( float distance )
	{
		HoldDistance += distance;
		HoldDistance = HoldDistance.Clamp( MinTargetDistance, MaxTargetDistance );
	}

	protected virtual void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x * RotateSpeed );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y * RotateSpeed );
		localRot = eye.Inverse * localRot;

		HeldRot = localRot * HeldRot;
	}

	public override void BuildInput()
	{
		if ( !Input.Down( "use" ) || !Input.Down( "attack1" ) ||
			 !GrabbedEntity.IsValid() )
		{
			return;
		}

		//
		// Lock view angles
		//
		if ( Owner is Player pl )
		{
			pl.ViewAngles = pl.OriginalViewAngles;
		}
	}

	public override bool IsUsable( Entity user )
	{
		return Owner == null || HeldBody.IsValid();
	}

	[ClientRpc]
	public void StopBeamSound()
	{
		BeamSound.Stop();
		BeamSoundPlaying = false;
	}

	public override void OnCarryDrop( Entity dropper )
	{
		if ( Input.Pressed( "drop" ) && Input.Down( "run" ) )
		{
			return;
		}
		GrabEnd();

		StopBeamSound( To.Single( dropper ) );

		base.OnCarryDrop( dropper );
	}
}
