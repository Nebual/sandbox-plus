[Spawnable, Library( "weapon_flashlight" )]
partial class Flashlight : BaseWeapon
{
	protected SpotLight WorldLight => GetComponentInChildren<SpotLight>( true );
	protected SpotLight ViewLight => ViewModel?.GetComponentInChildren<SpotLight>( true );

	[Sync, Change( nameof( ToggleLight ) )] private bool LightEnabled { get; set; } = true;

	TimeSince timeSinceLightToggled;
	public override bool ShouldShowCrosshair()
	{
		return false;
	}

	public override void OnControl()
	{
		base.OnControl();

		if ( ViewLight.IsValid() )
		{
			ViewLight.Enabled = LightEnabled;
		}

		bool toggle = Input.Pressed( "flashlight" ) || Input.Pressed( "attack1" );

		if ( timeSinceLightToggled > 0.1f && toggle )
		{
			LightEnabled = !LightEnabled;

			timeSinceLightToggled = 0;
		}
	}

	private void ToggleLight()
	{
		Sound.Play( LightEnabled ? "flashlight-on" : "flashlight-off", WorldPosition );

		if ( WorldLight.IsValid() )
		{
			WorldLight.Enabled = LightEnabled;
		}
	}

	public override bool CanReload()
	{
		return false;
	}

	public override void AttackSecondary()
	{
		if ( MeleeAttack() )
		{
			OnMeleeHit();
		}
		else
		{
			OnMeleeMiss();
		}

		BroadcastAttackSecondary();
	}

	[Rpc.Broadcast]
	private void BroadcastAttackSecondary()
	{
		Sound.Play( "rust_flashlight.attack", WorldPosition );
	}

	private bool MeleeAttack()
	{
		var ray = Owner.AimRay;

		var forward = ray.Forward;
		forward = forward.Normal;

		bool hit = false;

		foreach ( var tr in TraceMelee( ray.Position, ray.Position + forward * 80, 20.0f ) )
		{
			tr.Surface.DoBulletImpact( tr );

			hit = true;

			if ( !tr.GameObject.IsValid() ) continue;

			if ( tr.GameObject.Components.TryGet<PropHelper>( out var prop ) )
			{
				prop.BroadcastAddDamagingForce( forward * 80 * 100, 25 );
			}
			else if ( tr.GameObject.Root.Components.TryGet<Player>( out var player ) )
			{
				player.TakeDamage( 25 );
			}
		}

		return hit;
	}

	private void OnMeleeMiss()
	{
		ViewModel?.Renderer?.Set( "attack", true );
	}

	private void OnMeleeHit()
	{
		ViewModel?.Renderer?.Set( "attack_hit", true );
	}
}
