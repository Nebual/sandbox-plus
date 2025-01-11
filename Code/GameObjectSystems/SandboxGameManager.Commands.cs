using Sandbox.Events;

public partial class SandboxGameManager
{
	[ConCmd( "spawn" )]
	public static void Spawn( string modelname )
	{
		var player = Player.FindLocalPlayer();
		if ( !player.IsValid() )
			return;

		var tr = Game.ActiveScene.Trace.Ray( player.EyeTransform.Position, player.EyeTransform.Position + player.EyeTransform.Rotation.Forward * 500 )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( player.GameObject )
			.Run();

		var modelRotation = Rotation.From( new Angles( 0, player.EyeTransform.Rotation.Angles().yaw, 0 ) ) * Rotation.FromAxis( Vector3.Up, 180 );

		SpawnModel( modelname, tr.EndPosition, modelRotation, player.GameObject );
		Sandbox.Services.Stats.Increment( "spawn.model", 1, modelname );
	}

	static async void SpawnModel( string modelname, Vector3 endPos, Rotation modelRotation, GameObject playerObject )
	{
		// Does this look like a package?
		if ( modelname.Count( x => x == '.' ) == 1 && !modelname.EndsWith( ".vmdl", StringComparison.OrdinalIgnoreCase ) && !modelname.EndsWith( ".vmdl_c", StringComparison.OrdinalIgnoreCase ) )
		{
			modelname = await SpawnPackageModel( modelname, playerObject );
			if ( modelname == null )
				return;
		}

		var model = Model.Load( modelname );
		if ( model == null || model.IsError )
			return;

		var go = new GameObject
		{
			WorldPosition = endPos + Vector3.Down * model.PhysicsBounds.Mins.z,
			WorldRotation = modelRotation,
			Tags = { "solid" }
		};

		var prop = go.AddComponent<Prop>();
		prop.Model = model;

		if ( prop.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
		{
			renderer.CreateBoneObjects = true;
		}

		var propHelper = go.AddComponent<PropHelper>();

		var rb = propHelper.Rigidbody;
		if ( rb.IsValid() )
		{
			// If there's no physics model, create a simple OBB
			foreach ( var shape in rb.PhysicsBody.Shapes )
			{
				if ( !shape.IsMeshShape )
					continue;

				var newCollider = go.AddComponent<BoxCollider>();
				newCollider.Scale = model.PhysicsBounds.Size;
			}
		}

		go.NetworkSpawn( playerObject.Network.Owner );
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );
		IPropSpawnedEvent.Post( x => x.OnSpawned( prop ) );

		UndoSystem.Add( creator: playerObject.GetComponent<Player>(), callback: () => UndoSpawn( go, modelname ), prop: go );
	}

	static string UndoSpawn( GameObject obj, string modelName )
	{
		obj?.Destroy();

		return $"Undone spawning {modelName.Split( '/' ).Last().Replace( ".vmdl", "" )}";
	}

	static async Task<string> SpawnPackageModel( string packageName, GameObject source )
	{
		var package = await Package.Fetch( packageName, false );
		if ( package == null || package.TypeName != "model" || package.Revision == null )
		{
			// spawn error particles
			return null;
		}

		if ( !source.IsValid() ) return null; // source gameobject died or disconnected or something

		var model = package.GetMeta( "PrimaryAsset", "models/dev/error.vmdl" );

		// Download and mount the package (if needed)
		using ( Rpc.FilterExclude( c => c == Connection.Local ) )
			BroadcastMount( packageName ); // broadcast the mount to everyone else (local player has to await it, so can't wait for RPC)
		await package.MountAsync();

		return model;
	}

	[Rpc.Broadcast]
	public static void BroadcastMount( string packageName )
	{
		GameTask.MainThread().OnCompleted( async () =>
		{
			var package = await Package.Fetch( packageName, true );
			await package.MountAsync();
		} );
	}

	[ConCmd( "changemap" )]
	public static void ChangeMapCmd( string mapName, string who )
	{
		Analytics.Increment( "map.changed" );
		if ( who == "everyone" )
		{
			ChangeMapCmd( mapName );
			return;
		}
		using ( Rpc.FilterInclude( c => c == Connection.Local ) )
		{
			ChangeMapCmd( mapName );
		}
	}
	[Rpc.Broadcast]
	public static void ChangeMapCmd( string mapName )
	{
		GameTask.MainThread().OnCompleted( async () =>
		{
			var mapPackage = await Package.FetchAsync( mapName, false );
			if ( mapPackage == null ) return;

			var primaryAsset = mapPackage.GetMeta<string>( "PrimaryAsset" );
			if ( string.IsNullOrEmpty( primaryAsset ) ) return;
			if ( !mapPackage.IsMounted() )
			{
				HintFeed.AddHint( "map", $"Map {mapName} not loaded, mounting...", 3 );
			}
			await mapPackage.MountAsync();

			if ( primaryAsset.EndsWith( ".scene" ) )
			{
				// todo: we'll need to unload the old scene, and reload the default scene when leaving one of these...
				// lets disable this for now
				HintFeed.AddHint( "map", $"Map {mapName} is a scene, not supported yet (probably has its own camera/playermodel/etc)", 5 );
				return;
				// var sceneLoadOptions = new SceneLoadOptions { IsAdditive = true };
				// var sceneFile = mapPackage.GetMeta<SceneFile>( "PrimaryAsset" );
				// sceneLoadOptions.SetScene( sceneFile );
				// Game.ActiveScene.Load( sceneLoadOptions );
			}
			else
			{
				CustomMapInstance.Current.MapName = mapName;
			}

			await GameTask.Delay( 100 );
			var mapPlayerSpawner = Game.ActiveScene?.GetComponentInChildren<MapPlayerSpawner>();
			if ( !mapPlayerSpawner.IsValid() )
			{ // huh. wait longer I guess.
				await GameTask.Delay( 1000 );
				mapPlayerSpawner = Game.ActiveScene?.GetComponentInChildren<MapPlayerSpawner>();
			}
			mapPlayerSpawner?.RespawnPlayers();
		} );
	}
}
