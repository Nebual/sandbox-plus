using Sandbox;

public sealed class UndoSystem : GameObjectSystem<UndoSystem>
{
	public UndoSystem( Scene scene ) : base( scene )
	{
	}

	private static Dictionary<long, List<Undo>> Undos = new();

	private static List<Undo> Get(SteamId id)
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		return Undos[id];
	}

	private static Undo GetFirstAndRemove(SteamId id)
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		if ( Undos[id].Count == 0 ) return null;

		Undo undo = Undos[id][0];
		Undos[id].RemoveAt( 0 );

		return undo;
	}

	private static void AddUndo( SteamId id, Undo undo)
	{
		if ( !Undos.ContainsKey( id ) )
		{
			Undos.Add( id, new List<Undo>() );
		}

		Undos[id].Insert( 0, undo );
	}

	public static bool Remove( SteamId steamId, Undo undo )
	{
		if ( !Undos.ContainsKey( steamId ) )
			Undos.Add( steamId, new List<Undo>() );

		return Undos[steamId].Remove( undo );
	}

	public static int RemoveByGameObject(SteamId steamId, GameObject go)
	{
		if ( !Undos.ContainsKey( steamId ) )
			Undos.Add( steamId, new List<Undo>() );

		return Undos[steamId].RemoveAll( x => x.Prop == go );
	}

	[ConCmd( "undo" )]
	public static void PlayerUndo()
	{
		var player = Player.FindLocalPlayer();
		if ( !player.IsValid() )
			return;

		Undo undo = GetFirstAndRemove( player.SteamId );

		if (undo != null)
		{
			if ( undo.UndoCallback != null )
			{
				var undoMessage = undo.UndoCallback();
				if ( undoMessage != "" )
				{
					HintFeed.AddHint( "", undoMessage );

					if ( undo.Prop != null) CreateUndoParticles( undo.Prop );
					// Nostalgia
					Sound.Play( "drop_001", player.WorldPosition );
				}
			}
		}
	}

	public static void Add( Player creator, Func<string> callback, GameObject prop= null)
	{
		if ( creator == null ) return;

		var undo = new Undo( creator: creator, callback: callback, prop: prop);

		AddUndo( creator.SteamId, undo );
	}

	[Rpc.Broadcast]
	public static void CreateUndoParticles( GameObject go )
	{
		if ( go == null ) return;
		var effect = Sandbox.GameObject.Clone( "prefabs/effects/remove.prefab", new CloneConfig { StartEnabled = false } );
		effect?.WorldTransform = go.WorldTransform;
		var emitter = effect?.GetComponentInChildren<ParticleBoxEmitter>(true);
		var size = go.GetLocalBounds().Size;
		emitter?.Size = size;
		emitter?.Burst = float.Clamp(50 * (size.Length / 50f), 25, 1500);
		effect?.Enabled = true;
	}
}
