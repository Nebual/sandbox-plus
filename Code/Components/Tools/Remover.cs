[Library( "tool_remover", Title = "Remover", Description = "Remove entities", Group = "construction" )]
public class Remover : BaseTool
{
	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.GameObject.IsWorld() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			Remove( Owner.SteamId, trace.GameObject );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	static void Remove( SteamId steamId, GameObject go )
	{
		if ( !go.IsValid() ) return;
		UndoSystem.RemoveByGameObject(steamId, go);
		UndoSystem.CreateUndoParticles(go);
		go.Destroy();
	}
}
