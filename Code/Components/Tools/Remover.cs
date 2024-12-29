[Library( "tool_remover", Description = "Remove entities", Group = "construction" )]
public class Remover : BaseTool
{
	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.GameObject.IsWorld() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			Remove( trace.GameObject );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	static void Remove( GameObject g )
	{
		g.Destroy();

		Particles.MakeParticleSystem( "particles/physgun_freeze.vpcf", g.WorldTransform );
	}
}
