using Sandbox.Citizen;
using Sandbox.Network;
using Sandbox.Services;

public partial class SandboxGameManager : GameObjectSystem<SandboxGameManager>, IPlayerEvent, Component.INetworkListener, ISceneStartup
{
	struct PlayerConnectionData
	{
		public string ClientPassword { get; set; }
	}
	
	[ConVar("sbox_password", Help = "Sets the password for the sandbox.")]
	public static String Password { get; set; } = "";
	
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
				Networking.CreateLobby( new LobbyConfig { Name = $"{Package.GetCachedTitle( Game.Ident )} Server" } );
			}
		}
		InitAddons( true );
	}
	void ISceneStartup.OnClientInitialize()
	{
		if ( Networking.IsHost ) return; // already done in OnHostInitialize
		InitAddons( false );
	}

	private void InitAddons( bool isHostInit )
	{
		var methods = TypeLibrary.GetMethodsWithAttribute<GameInitAttribute>();
		foreach ( var (method, attribute) in methods )
		{
			if ( attribute.HostOnly && !isHostInit ) continue;
			if ( method.IsStatic )
			{
				method.Invoke( null, null );
			}
		}
	}

	[Rpc.Broadcast(NetFlags.Reliable & NetFlags.SendImmediate)]
	private static void OnRequestPlayerConnectionData( SteamId playerId )
	{
		if ( Connection.Local.SteamId == playerId )
		{
			// In the editor (and potentially other scenarios) the password is invalid
			var password = Password;
			if ( password == null )
			{
				password = ConsoleSystem.GetValue( "sbox_password" );

				if ( password == null )
				{
					password = ConsoleSystem.GetValue("sv_password");
				}
			}
			var data = new PlayerConnectionData
			{
				ClientPassword = password
			};
			
			Log.Info($"Sending player connection data");
			
			OnPlayerConnectionDataReceived(data);
		}
	}
	
	[Rpc.Host(NetFlags.Reliable & NetFlags.SendImmediate)]
	private static void OnPlayerConnectionDataReceived( PlayerConnectionData data )
	{
		Log.Info($"Player connection data received for {Rpc.Caller.SteamId}");
		if ( Password.Length > 0 && data.ClientPassword != Password )
		{
			Log.Info($"{data.ClientPassword}, {Password}");
			Rpc.Caller.Kick("Incorrect password");
		}
	}

	bool Component.INetworkListener.AcceptConnection( Connection channel, ref string reason )
	{
		// Always accept the host
		if ( channel.IsHost )
			return true;
		
		// Checking the password here would be preferable, but Facepunch has locked down
		// all the functions that would let us acquire the data we need at this point
		/*if ( Password.Length == 0 )
			return true;

		var passwordRequest = channel.SendRequest( "sbox_request_password" );
		var startTime = RealTime.Now;
		while ( !passwordRequest.IsCompleted )
		{
			if ( RealTime.Now - startTime > 1.0f )
			{
				reason = "Player data request timed out";
				return false;
			}
		}
		
		var userPassword = passwordRequest.Result as string;
		if ( userPassword != Password )
		{
			reason = "Incorrect password";
			return false;
		}*/
		
		return true;
	}

	void Component.INetworkListener.OnActive( Connection channel )
	{
		OnRequestPlayerConnectionData(channel.SteamId);
		Log.Info($"Requesting player data for {channel.SteamId}");
		
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

		player.Name = channel.DisplayName;
		player.SteamId = channel.SteamId;
		player.PartyId = channel.PartyId;

		//Make sure all of these exist
		var animHelper = playerGo.Components.GetInDescendantsOrSelf<CitizenAnimationHelper>();
		var controller = playerGo.Components.GetOrCreate<SandboxPlus.PlayerController>();
		var inventory = playerGo.Components.GetOrCreate<PlayerInventory>();

		if ( animHelper.IsValid() )
			player.Body = animHelper.GameObject;

		if ( controller.IsValid() )
			player.Controller = controller;

		if ( inventory.IsValid() )
			player.Inventory = inventory;

		if (Networking.IsHost) {
			channel.CanRefreshObjects = true;
		}

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
