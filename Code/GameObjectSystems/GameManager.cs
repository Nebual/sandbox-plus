public sealed partial class GameManager : GameObjectSystem<GameManager>, IPlayerEvent, Component.INetworkListener, ISceneStartup
{
	public GameManager( Scene scene ) : base( scene )
	{
	}

	void ISceneStartup.OnHostPreInitialize( SceneFile scene )
	{
		Log.Info( $"{Package.GetCachedTitle(Game.Ident)}: Loading scene {scene.ResourceName}" );
	}

	void Component.INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		if ( Game.ActiveScene.GetAllComponents<Player>().Any( x => x.Network.Owner == channel ) )
		{
			Log.Info( "GameManager: Tried to spawn multiple instances of the same player! Ignoring." );
			return;
		}

		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var playerGo = GameObject.Clone( "/prefabs/player.prefab", new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var player = playerGo.Components.Get<Player>( true );

		player.Name = channel.Name;
		player.SteamId = channel.SteamId;
		player.PartyId = channel.PartyId;

		playerGo.NetworkSpawn( channel );

		IPlayerEvent.PostToGameObject( player.GameObject, x => x.OnSpawned() );
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return Transform.Zero;
	}
}
