namespace Sandbox
{
	public class CanToolParams
	{
		public Player player;
		public string toolName;
		public TraceResult tr;
		public Entity entity;
		public bool preventDefault = false;

		public static TraceResult RunCanTool( Player player, string toolName, TraceResult tr )
		{
			if ( Game.IsClient )
			{
				return tr; // PlayerOwner table is currently serverside only
			}
			if ( !tr.Entity.IsValid() || tr.Entity.IsWorld )
			{
				return tr;
			}
			var canToolParams = new CanToolParams
			{
				player = player,
				toolName = toolName,
				tr = tr,
				entity = tr.Entity,
			};
			Event.Run( "player.cantool", canToolParams );
			if ( canToolParams.preventDefault )
			{
				return new TraceResult();
			}
			else
			{
				return tr;
			}
		}
	}
}
