using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

[Spawnable]
[Library( "gravgun" )]
public partial class GravGun : Carriable
{
	public override string ViewModelPath => Cloud.Asset( "katka/gravitygun" );
	private AnimatedEntity ViewModelArms { get; set; }
	private AnimatedEntity ArmsAdapter { get; set; }
	public List<CapsuleLightEntity> LightsWorld;
	public PointLightEntity LightView;
	public Color CrystalColor { get; set; } = Color.FromBytes( 172, 64, 0 );

	public PhysicsBody HeldBody { get; private set; }
	public Vector3 HeldPos { get; private set; }
	public Rotation HeldRot { get; private set; }
	public ModelEntity HeldEntity { get; private set; }
	public Vector3 HoldPos { get; private set; }
	public Rotation HoldRot { get; private set; }

	protected virtual float MaxPullDistance => 2000.0f;
	protected virtual float MaxPushDistance => 500.0f;
	protected virtual float LinearFrequency => 10.0f;
	protected virtual float LinearDampingRatio => 1.0f;
	protected virtual float AngularFrequency => 10.0f;
	protected virtual float AngularDampingRatio => 1.0f;
	protected virtual float PullForce => 20.0f;
	protected virtual float PushForce => 1000.0f;
	protected virtual float ThrowForce => 2000.0f;
	protected virtual float HoldDistance => 50.0f;
	protected virtual float AttachDistance => 150.0f;
	protected virtual float DropCooldown => 0.5f;
	protected virtual float BreakLinearForce => 2000.0f;

	private TimeSince timeSinceDrop;

	private const string grabbedTag = "grabbed";

	[Net]
	private float ProngsState { get; set; } = 0;
	[Net]
	private bool ProngsActive { get; set; } = false;

	private bool hasPlayedProngsOpenSound = false;
	private bool hasPlayedProngsCloseSound = false;
	private bool hasPlayedTooHeavySound = false;
	private Sound TooHeavySound;
	private bool hasPlayedDryFireSound = false;
	private Sound DryFireSound;

	private Sound HoldLoopSound;
	private bool shouldPlayHoldSound = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ViewModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		Tags.Add( "weapon", "solid" );
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
		ViewModelEntity.SetBodyGroup( "crystal_inside", 1 );

		ArmsAdapter = new AnimatedEntity( Cloud.Asset( "katka/hand_adapter_valvebiped_to_sbox" ) );
		ArmsAdapter.SetParent( ViewModelEntity, true );
		ArmsAdapter.EnableViewmodelRendering = ViewModelEntity.EnableViewmodelRendering;

		ViewModelArms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
		ViewModelArms.SetParent( ArmsAdapter, true );
		ViewModelArms.EnableViewmodelRendering = ViewModelEntity.EnableViewmodelRendering;
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

		SetAnimParameter( "prongs", ProngsState );
		ViewModelEntity?.SetAnimParameter( "prongs", ProngsState );

		ViewModelEntity?.SetAnimParameter( "moveback", 0.85f );

		if ( !Game.IsServer )
			return;

		ProngsState = ProngsState.LerpTo( ProngsActive ? 1 : 0, Time.Delta * 10f );

		using ( Prediction.Off() )
		{
			if ( shouldPlayHoldSound )
			{
				if ( !HoldLoopSound.IsPlaying ) HoldLoopSound = owner.PlaySound( "sounds/weapons/gravity_gun/hold_loop.sound" );
			}
			else
			{
				HoldLoopSound.Stop();
			}

			var eyePos = owner.EyePosition;
			var eyeRot = owner.EyeRotation;
			var eyeDir = owner.EyeRotation.Forward;

			if ( HeldBody.IsValid() && HeldBody.PhysicsGroup != null )
			{
				if ( Input.Pressed( "attack1" ) )
				{
					if ( HeldBody.PhysicsGroup.BodyCount > 1 )
					{
						// Don't throw ragdolls as hard
						HeldBody.PhysicsGroup.ApplyImpulse( eyeDir * (ThrowForce * 0.5f), true );
						HeldBody.PhysicsGroup.ApplyAngularImpulse( Vector3.Random * ThrowForce, true );
					}
					else
					{
						HeldBody.ApplyImpulse( eyeDir * (HeldBody.Mass * ThrowForce) );
						HeldBody.ApplyAngularImpulse( Vector3.Random * (HeldBody.Mass * ThrowForce) );
					}

					GrabEnd();

					SetViewModelParam( To.Single( owner ), "altfire" );
					owner.PlaySound( "sounds/weapons/gravity_gun/superphys_launch1.sound" );
					shouldPlayHoldSound = false;
				}
				else if ( Input.Pressed( "attack2" ) )
				{
					GrabEnd();

					SetViewModelParam( To.Single( owner ), "drop" );
					owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_drop.sound" );
					shouldPlayHoldSound = false;
				}
				else
				{
					GrabMove( eyePos, eyeDir, eyeRot );
				}

				ProngsActive = true;

				return;
			}

			if ( timeSinceDrop < DropCooldown )
				return;

			// Prongs open/close sounds (messy)
			if ( ProngsActive )
			{
				if ( !hasPlayedProngsOpenSound )
				{
					owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_claws_open.sound" );
					hasPlayedProngsOpenSound = true;
					hasPlayedProngsCloseSound = false;
				}
			}
			else
			{
				if ( !hasPlayedProngsCloseSound )
				{
					owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_claws_close.sound" );
					hasPlayedProngsCloseSound = true;
					hasPlayedProngsOpenSound = false;
				}
			}

			ProngsActive = false;

			var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxPullDistance )
				.UseHitboxes()
				.WithAnyTags( "solid", "debris", "nocollide" )
				.Ignore( this )
				.Radius( 2.0f )
				.Run();

			if ( !tr.Hit || !tr.Body.IsValid() || !tr.Entity.IsValid() || tr.Entity.IsWorld )
			{
				if ( Input.Down( "attack2" ) )
				{
					if ( !hasPlayedTooHeavySound )
					{
						if ( TooHeavySound.IsPlaying )
						{
							TooHeavySound.Stop();
						}

						TooHeavySound = owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_tooheavy.sound" );
						hasPlayedTooHeavySound = true;
					}
				}
				else if ( Input.Down( "attack1" ) )
				{
					if ( !hasPlayedDryFireSound )
					{
						if ( DryFireSound.IsPlaying )
						{
							DryFireSound.Stop();
						}

						DryFireSound = owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_dryfire.sound" );
						hasPlayedDryFireSound = true;
						SetViewModelParam( To.Single( owner ), "fire" );
					}
				}
				else
				{
					// TooHeavySound.Stop();
					hasPlayedTooHeavySound = false;
					hasPlayedDryFireSound = false;
				}

				return;
			}

			if ( tr.Entity.PhysicsGroup == null )
			{
				HoldLoopSound.Stop();
				return;
			}

			var modelEnt = tr.Entity as ModelEntity;
			if ( !modelEnt.IsValid() )
			{
				HoldLoopSound.Stop();
				return;
			}

			if ( modelEnt.Tags.Has( grabbedTag ) )
			{
				HoldLoopSound.Stop();
				return;
			}

			var body = tr.Body;

			if ( body.BodyType != PhysicsBodyType.Dynamic )
			{
				HoldLoopSound.Stop();
				return;
			}

			if ( eyePos.Distance( modelEnt.CollisionWorldSpaceCenter ) < AttachDistance )
				ProngsActive = true;

			if ( Input.Pressed( "attack1" ) )
			{
				if ( tr.Distance < MaxPushDistance )
				{
					var pushScale = 1.0f - Math.Clamp( tr.Distance / MaxPushDistance, 0.0f, 1.0f );
					body.ApplyImpulseAt( tr.EndPosition, eyeDir * (body.Mass * (PushForce * pushScale)) );

					SetViewModelParam( To.Single( owner ), "fire" );
					owner.PlaySound( "sounds/weapons/gravity_gun/superphys_launch1.sound" );
				}
			}
			else if ( Input.Down( "attack2" ) )
			{
				var physicsGroup = tr.Entity.PhysicsGroup;

				if ( physicsGroup.BodyCount > 1 )
				{
					body = modelEnt.PhysicsBody;
					if ( !body.IsValid() )
						return;
				}

				var attachPos = body.FindClosestPoint( eyePos );

				if ( eyePos.Distance( attachPos ) <= AttachDistance )
				{
					var holdDistance = HoldDistance + attachPos.Distance( body.MassCenter );
					GrabStart( modelEnt, body, eyePos + eyeDir * holdDistance, eyeRot );

					SetViewModelParam( To.Single( owner ), "hold" );
					owner.PlaySound( "sounds/weapons/gravity_gun/physcannon_pickup.sound" );
					shouldPlayHoldSound = true;
				}
				else
				{
					physicsGroup.ApplyImpulse( eyeDir * -PullForce, true );
				}
			}
		}
	}

	private void Activate()
	{
	}

	private void Deactivate()
	{
		GrabEnd();
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		ViewModelEntity?.SetAnimParameter( "deploy", true );

		if ( Game.IsServer )
		{
			Activate();
		}

		if ( Game.IsClient )
		{
			CreateLights();
		}
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		if ( Game.IsServer )
		{
			Deactivate();
		}

		if ( Game.IsClient )
		{
			DestroyLights();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Game.IsServer )
		{
			Deactivate();
		}
	}

	[Event.Physics.PreStep]
	public void OnPrePhysicsStep()
	{
		if ( !Game.IsServer )
			return;

		if ( !HeldBody.IsValid() )
			return;

		if ( HeldEntity is Player )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.1f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.1f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	private void GrabStart( ModelEntity entity, PhysicsBody body, Vector3 grabPos, Rotation grabRot )
	{
		if ( !body.IsValid() )
			return;

		if ( body.PhysicsGroup == null )
			return;

		GrabEnd();

		HeldBody = body;
		HeldPos = HeldBody.LocalMassCenter;
		HeldRot = grabRot.Inverse * HeldBody.Rotation;

		HoldPos = HeldBody.Position;
		HoldRot = HeldBody.Rotation;

		HeldBody.Sleeping = false;
		HeldBody.AutoSleep = false;

		HeldEntity = entity;
		HeldEntity.Tags.Add( grabbedTag );

		Client?.Pvs.Add( HeldEntity );
	}

	private void GrabEnd()
	{
		timeSinceDrop = 0;

		if ( HeldBody.IsValid() )
		{
			HeldBody.AutoSleep = true;
		}

		if ( HeldEntity.IsValid() )
		{
			Client?.Pvs.Remove( HeldEntity );
		}

		HeldBody = null;
		HeldRot = Rotation.Identity;

		if ( HeldEntity.IsValid() )
		{
			HeldEntity.Tags.Remove( grabbedTag );
		}

		HeldEntity = null;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot )
	{
		if ( !HeldBody.IsValid() )
			return;

		var attachPos = HeldBody.FindClosestPoint( startPos );
		var holdDistance = HoldDistance + attachPos.Distance( HeldBody.MassCenter );

		HoldPos = startPos - HeldPos * HeldBody.Rotation + dir * holdDistance;
		HoldRot = rot * HeldRot;

		//ProngsActive = true;
	}

	public override bool IsUsable( Entity user )
	{
		return Owner == null || HeldBody.IsValid();
	}

	public override void OnCarryDrop( Entity dropper )
	{
		GrabEnd();

		base.OnCarryDrop( dropper );
	}
}
