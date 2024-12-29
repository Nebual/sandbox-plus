/// <summary>
/// Holds player information like health
/// </summary>
public sealed partial class Player : Component, Component.IDamageable, PlayerController.IEvents
{
	public static Player FindLocalPlayer()
	{
		return Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
	}

	[RequireComponent] public PlayerController Controller { get; set; }
	[RequireComponent] public PlayerInventory Inventory { get; set; }

	[Property] public GameObject Body { get; set; }
	[Property, Range( 0, 100 ), Sync] public float Health { get; set; } = 100;

	[Property] public string Name { get; set; } = "Unknown";
	[Property] public SteamId SteamId { get; set; } = 0UL;
	[Property] public SteamId PartyId { get; set; } = 0UL;

	public bool IsDead => Health <= 0;
	public Transform EyeTransform => Controller.EyeTransform;
	public Ray AimRay => new( EyeTransform.Position, EyeTransform.Rotation.Forward );
	public bool SuppressScrollWheelInventory { get; set; } = false;

	public bool isInParty()
	{
		return PartyId != 0UL;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !IsProxy )
		{
			if ( Input.Pressed( "undo" ) )
			{
				UndoSystem.PlayerUndo();
			}
			if ( Input.Pressed( "attack3" ) )
			{
				SboxToolAuto();
			}
		}
	}

	/// <summary>
	/// Creates a ragdoll but it isn't enabled
	/// </summary>
	[Rpc.Broadcast]
	void CreateRagdoll()
	{
		if ( IsProxy ) return;

		var ragdoll = Controller.CreateRagdoll();
		if ( !ragdoll.IsValid() ) return;

		var corpse = ragdoll.AddComponent<PlayerCorpse>();
		corpse.Connection = Network.Owner;
		corpse.Created = DateTime.Now;

		ragdoll.NetworkSpawn( Rpc.Caller );
	}

	[Rpc.Broadcast]
	void CreateRagdollAndGhost()
	{
		if ( IsProxy ) return;

		var go = new GameObject( false, "Observer" );
		go.Components.Create<PlayerObserver>();
		go.NetworkSpawn( Rpc.Caller );
	}

	[Rpc.Broadcast]
	public void TakeDamage( float amount )
	{
		if ( IsProxy ) return;
		if ( IsDead ) return;

		Health -= amount;

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnTakeDamage( amount ) );

		if ( IsDead )
		{
			Health = 0;
			Death();
		}
	}

	[ConCmd( "kill" )]
	public static void KillCommand()
	{
		Player.FindLocalPlayer().TakeDamage( 99999 );
	}

	void Death()
	{
		CreateRagdoll();
		CreateRagdollAndGhost();

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnDied() );

		GameObject.Destroy();
	}

	void IDamageable.OnDamage( in DamageInfo damage )
	{
		TakeDamage( damage.Damage );
	}

	void PlayerController.IEvents.OnEyeAngles( ref Angles ang )
	{
		var player = Components.Get<Player>();
		var angles = ang;
		ILocalPlayerEvent.Post( x => x.OnCameraMove( ref angles ) );
		ang = angles;
	}

	void PlayerController.IEvents.PostCameraSetup( CameraComponent camera )
	{
		var player = Components.Get<Player>();

		camera.FieldOfView = Screen.CreateVerticalFieldOfView( Preferences.FieldOfView );

		ILocalPlayerEvent.Post( x => x.OnCameraSetup( camera ) );
		ILocalPlayerEvent.Post( x => x.OnCameraPostSetup( camera ) );
	}

	void PlayerController.IEvents.OnLanded( float distance, Vector3 impactVelocity )
	{
		var player = Components.Get<Player>();
		IPlayerEvent.PostToGameObject( GameObject, x => x.OnLand( distance, impactVelocity ) );
	}

	public static SceneTraceResult DoBasicTrace()
	{
		var player = Player.FindLocalPlayer();
		var trace = player.Scene.Trace.Ray( player.AimRay.Position, player.AimRay.Position + player.AimRay.Forward * 5000 )
				.UseHitboxes()
				.WithAnyTags( "solid", "npc", "glass" )
				.WithoutTags( "debris", "player" )
				.IgnoreGameObjectHierarchy( player.GameObject )
				.Size( 2 );

		return trace.Run();
	}

	public static Player FindPlayerByConnection( Connection connection )
	{
		return Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.Owner == connection );
	}
	public static Player FindPlayerOwner( GameObject go )
	{
		return FindPlayerByConnection( go.Network.Owner );
	}
}
