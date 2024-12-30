using Sandbox.Citizen;
using Sandbox.Network;
using Sandbox.Services;

public partial class SandboxGameManager : GameObjectSystem<SandboxGameManager>, IPlayerEvent, Component.INetworkListener, ISceneStartup
{
	public SandboxGameManager( Scene scene ) : base( scene )
	{
	}

	void ISceneStartup.OnHostPreInitialize( SceneFile scene )
	{
		Log.Info( $"{Package.GetCachedTitle(Game.Ident)}: Loading scene {scene.ResourceName}" );
	}

	async void ISceneStartup.OnHostInitialize()
	{
		// NOTE: See CreateGameModal.razor, line 73 for a related issue

		var currentScene = Scene.Source as SceneFile;
		if ( currentScene.GetMetadata( "Title" ) == "game" )
		{
			// If the map is a scene, load it
			var mapPackage = await Package.FetchAsync( CustomMapInstance.Current.MapName, false );
			if ( mapPackage == null ) return;

			var primaryAsset = mapPackage.GetMeta<string>( "PrimaryAsset" );
			if ( string.IsNullOrEmpty( primaryAsset ) ) return;

			var sceneLoadOptions = new SceneLoadOptions { IsAdditive = true };

			if ( primaryAsset.EndsWith( ".scene" ) )
			{
				var sceneFile = mapPackage.GetMeta<SceneFile>( "PrimaryAsset" );
				sceneLoadOptions.SetScene( sceneFile );
				Scene.Load( sceneLoadOptions );
			}

			sceneLoadOptions.SetScene( "scenes/engine.scene" );
			Scene.Load( sceneLoadOptions );

			if ( !Networking.IsActive )
			{
				Networking.CreateLobby( new LobbyConfig { Name = $"{Package.GetCachedTitle(Game.Ident)} Server" } );
			}
		}
	}

	void Component.INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		if ( Scene.GetAllComponents<Player>().Any( x => x.Network.Owner == channel ) )
		{
			Log.Info( "SandboxGameManager: Tried to spawn multiple instances of the same player! Ignoring." );
			return;
		}

		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var playerGo = GameObject.Clone( "/prefabs/player.prefab", new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var player = playerGo.Components.GetOrCreate<Player>();

		player.Name = channel.Name;
		player.SteamId = channel.SteamId;
		player.PartyId = channel.PartyId;

		//Make sure all of these exist
		var animHelper = playerGo.Components.GetInDescendantsOrSelf<CitizenAnimationHelper>();
		var controller = playerGo.Components.GetOrCreate<PlayerController>();
		var inventory = playerGo.Components.GetOrCreate<PlayerInventory>();

		if ( animHelper.IsValid() )
			player.Body = animHelper.GameObject;

		if ( controller.IsValid() )
			player.Controller = controller;

		if ( inventory.IsValid() )
			player.Inventory = inventory;

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
