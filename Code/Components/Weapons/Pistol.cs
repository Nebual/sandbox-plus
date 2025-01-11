[Spawnable, Library( "weapon_pistol" )]
partial class Pistol : BaseWeapon, Component.ICollisionListener
{
	public RealTimeSince TimeSinceDischarge { get; set; }
	[Sync] protected bool WeagleMode { get; set; } = false;

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( "attack1" );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		// an old joke, sorry for putting this here for all to see lol
		if ( WeagleMode ) { WeagleAttack(); return; }

		BroadcastAttackPrimary();

		ViewModel?.Renderer?.Set( "b_attack", true );

		ShootEffects();
		ShootBullet( 0.05f, 1.5f, 9.0f, 3.0f );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Controller?.Renderer?.Set( "b_attack", true );
		Sound.Play( "rust_pistol.shoot", WorldPosition );
	}

	public override bool CanSecondaryAttack()
	{
		return Input.Pressed( "attack2" );
	}

	public override void AttackSecondary()
	{
		if ( Input.Down( "run" ) )
		{
			WeagleMode = !WeagleMode;
			ViewModel?.Renderer.SetClientMaterialOverride( WeagleMode ? "sugmatextures.stylizedwoodplanks" : "" );
			WorldModel?.SetClientMaterialOverride( WeagleMode ? "sugmatextures.stylizedwoodplanks" : "" );
		}
	}

	protected void WeagleAttack()
	{
		var tr = Player.DoBasicTrace();
		if ( tr.GameObject.IsValid() && tr.GameObject.GetComponent<PropHelper>() is PropHelper prop )
		{
			BroadcastAttackPrimary();
			ViewModel?.Renderer?.Set( "b_attack", true );
			WeagleKill( prop );
			ShootEffects();
			Analytics.Increment( "fun.weagle.kill" );
		}
	}

	[Rpc.Broadcast]
	protected void WeagleKill( PropHelper prop )
	{
		prop.Prop.Model = Model.Load( "models/citizen_props/crate01.vmdl" );
		prop.Prop.Kill();
	}
}
