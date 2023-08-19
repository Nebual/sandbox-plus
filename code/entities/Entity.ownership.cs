using System.Collections.Generic;
using Sandbox;

public static class EntityOwnershipExtensions
{
	public static Dictionary<Entity, Player> PlayerOwners { get; set; } = new();
	public static Player GetPlayerOwner( this Entity ent )
	{
		Game.AssertServer();
		return PlayerOwners.GetValueOrDefault( ent );
	}
	public static void SetPlayerOwner( this Entity ent, Player player )
	{
		Game.AssertServer();
		PlayerOwners[ent] = player;
	}
}
