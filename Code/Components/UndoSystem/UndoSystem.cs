using Sandbox;
using Sandbox.Diagnostics;

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

	private static void Add(SteamId id, Undo undo)
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
					CreateUndoParticles( Vector3.Zero );
				}
			}
		}
	}

	public static void Add( Player creator, Func<string> callback, bool avoid = false )
	{
		if ( creator == null ) return;

		var undo = new Undo( creator )
		{
			UndoCallback = callback,
			Avoid = avoid
		};

		Add( creator.SteamId, undo );
	}

	[Rpc.Broadcast]
	public static void CreateUndoParticles( Vector3 pos )
	{
		if ( pos != Vector3.Zero )
		{
			Particles.MakeParticleSystem( "particles/physgun_freeze.vpcf", new Transform( pos ), 4 );
		}
	}
}
