using Sandbox.Citizen;

/// <summary>
/// A common base we can use for weapons so we don't have to implement the logic over and over
/// again. Feel free to not use this and to implement it however you want to.
/// </summary>
[Icon( "sports_martial_arts" )]
public partial class BaseWeapon : Component
{
	[Property] public GameObject ViewModelPrefab { get; set; }
	[Property] public string ParentBone { get; set; } = "hold_r";
	[Property] public Transform BoneOffset { get; set; } = new Transform( 0 );
	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.HoldItem;
	[Property] public CitizenAnimationHelper.Hand Handedness { get; set; } = CitizenAnimationHelper.Hand.Right;
	[Property] public float PrimaryRate { get; set; } = 5.0f;
	[Property] public float SecondaryRate { get; set; } = 15.0f;
	[Property] public float ReloadTime { get; set; } = 3.0f;

	[Sync] public bool IsReloading { get; set; }
	[Sync] public RealTimeSince TimeSinceReload { get; set; }
	[Sync] public RealTimeSince TimeSinceDeployed { get; set; }
	[Sync] public RealTimeSince TimeSincePrimaryAttack { get; set; }
	[Sync] public RealTimeSince TimeSinceSecondaryAttack { get; set; }

	public ViewModel ViewModel => Scene?.Camera?.GetComponentInChildren<ViewModel>( true );
	public SkinnedModelRenderer WorldModel => GameObject?.GetComponentInChildren<SkinnedModelRenderer>( true );
	public SkinnedModelRenderer LocalWorldModel => !Owner.IsValid() || !Owner.Controller.IsValid() || Owner.Controller.ThirdPerson || IsProxy ? WorldModel : ViewModel?.Renderer;
	public Player Owner => GameObject?.Root?.GetComponent<Player>();

	public Transform Attachment( string name )
	{
		return LocalWorldModel?.GetAttachment( name ) ?? WorldTransform;
	}

	public virtual bool ShouldShowCrosshair()
	{
		return true;
	}

	protected override void OnAwake()
	{
		var obj = Owner?.Controller?.Renderer?.GetBoneObject( ParentBone );
		if ( obj is not null )
		{
			GameObject.Parent = obj;
			GameObject.LocalTransform = BoneOffset.WithScale( 1 );
		}
	}

	protected override void OnEnabled()
	{
		TimeSinceDeployed = 0;

		BroadcastEnabled();

		if ( IsProxy ) return;

		var go = ViewModelPrefab?.Clone( new CloneConfig()
		{
			StartEnabled = true,
			Parent = Scene.Camera.GameObject,
			Transform = Scene.Camera.WorldTransform
		} );

		go.NetworkMode = NetworkMode.Never;
	}

	[Rpc.Broadcast]
	private void BroadcastEnabled()
	{
		Owner?.Controller?.Renderer?.Set( "b_deploy", true );
	}

	protected void SetRendererAnimParam( string param, bool value )
	{
		if ( !IsProxy )
		{
			ViewModel?.Renderer.Set( param, value );
		}
	}
	protected void SetRendererAnimParam( string param, float value )
	{
		if ( !IsProxy )
		{
			ViewModel?.Renderer.Set( param, value );
		}
	}

	protected override void OnDisabled()
	{
		if ( IsProxy ) return;

		ViewModel.GameObject.DestroyImmediate();
	}

	protected override void OnUpdate()
	{
		GameObject.NetworkInterpolation = false;

		Owner?.Controller?.Renderer?.Set( "holdtype", (int)HoldType );
		Owner?.Controller?.Renderer?.Set( "holdtype_handedness", (int)Handedness );

		if ( IsProxy )
			return;

		Scene.Camera.Tags.Set( "viewer", Owner.Controller.ThirdPerson );

		OnControl();
	}

	public virtual void OnControl()
	{
		if ( TimeSinceDeployed < 0.6f )
			return;

		if ( !IsReloading )
		{
			if ( IsProxy )
				return;

			if ( CanReload() )
			{
				Reload();
			}

			//
			// Reload could have changed our owner
			//
			if ( !Owner.IsValid() )
				return;

			if ( CanPrimaryAttack() )
			{
				TimeSincePrimaryAttack = 0;
				AttackPrimary();
			}

			//
			// AttackPrimary could have changed our owner
			//
			if ( !Owner.IsValid() )
				return;

			if ( CanSecondaryAttack() )
			{
				TimeSinceSecondaryAttack = 0;
				AttackSecondary();
			}
		}

		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}
	}

	public virtual void OnReloadFinish()
	{
		IsReloading = false;
	}

	public virtual void StartReloadEffects()
	{
		ViewModel?.Renderer?.Set( "b_reload", true );
	}

	protected virtual void ShootEffects()
	{
		AttachParticleSystem( "particles/pistol_muzzleflash.vpcf", "muzzle" );
		ViewModel?.Renderer?.Set( "fire", true );
	}

	[Rpc.Broadcast]
	public void AttachParticleSystem( string path, string attachment, float time = 1, GameObject parent = null )
	{
		Transform transform = LocalWorldModel?.GetAttachment( attachment ) ?? WorldTransform;

		Particles.MakeParticleSystem( path, transform, time, parent );
	}

	public virtual bool CanReload()
	{
		if ( Owner == null || !Input.Down( "reload" ) ) return false;

		return true;
	}

	public virtual void Reload()
	{
		if ( IsReloading )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		BroadcastReload();
		StartReloadEffects();
	}

	[Rpc.Broadcast]
	private void BroadcastReload()
	{
		Owner?.Controller?.Renderer?.Set( "b_reload", true );
	}

	public virtual bool CanPrimaryAttack()
	{
		if ( Owner == null || !Input.Down( "attack1" ) ) return false;

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public virtual void AttackPrimary()
	{

	}

	public virtual bool CanSecondaryAttack()
	{
		if ( Owner == null || !Input.Down( "attack2" ) ) return false;

		var rate = SecondaryRate;
		if ( rate <= 0 ) return true;

		return TimeSinceSecondaryAttack > (1 / rate);
	}

	public virtual void AttackSecondary()
	{

	}

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocheting or something.
	/// </summary>
	public virtual IEnumerable<SceneTraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		// bool underWater = Trace.TestPoint( start, "water" );

		var trace = Scene.Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass" )
				.WithoutTags( "playercontroller", "debris" )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		/*
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );
		*/

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	public IEnumerable<SceneTraceResult> TraceMelee( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var trace = Scene.Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass" )
				.WithoutTags( "playercontroller", "debris" )
				.IgnoreGameObjectHierarchy( GameObject.Root );

		var tr = trace.Run();

		if ( tr.Hit )
		{
			yield return tr;
		}
		else
		{
			trace = trace.Size( radius );

			tr = trace.Run();

			if ( tr.Hit )
			{
				yield return tr;
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize )
	{
		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( pos, pos + forward * 5000, bulletSize ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !tr.GameObject.IsValid() ) continue;

			if ( tr.GameObject.Components.TryGet<PropHelper>( out var prop ) )
			{
				prop.BroadcastAddDamagingForce( forward * 5000 * force, damage );
			}
			else if ( tr.GameObject.Root.Components.TryGet<Player>( out var player ) )
			{
				player.TakeDamage( damage );
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;
		ShootBullet( ray.Position, ray.Forward, spread, force, damage, bulletSize );
	}

	/// <summary>
	/// Shoot a multiple bullets from owners view point
	/// </summary>
	public virtual void ShootBullets( int numBullets, float spread, float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;

		for ( int i = 0; i < numBullets; i++ )
		{
			ShootBullet( ray.Position, ray.Forward, spread, force / numBullets, damage, bulletSize );
		}
	}
}
