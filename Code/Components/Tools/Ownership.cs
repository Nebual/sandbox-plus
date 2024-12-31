[Library( "tool_ownership", Title = "Ownership", Description = "Transfers network ownership (simulation) of props", Group = "rendering" )]
public partial class OwnershipTool : BaseTool
{
	protected override void OnEnabled()
	{
		base.OnEnabled();
		LongDescription = "Transfers network ownership (simulation) of props\nPrimary: Claim ownership of a prop (Shift: include constrained props)";
	}
	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) ) return false;
		if ( !trace.Hit || !trace.GameObject.IsValid() ) return false;

		if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
			return false; // probably only want to work with props for now

		if ( trace.GameObject.Network.OwnerTransfer == OwnerTransfer.Fixed )
		{
			HintFeed.AddHint( "cross", "OwnerTransfer is locked for that prop", 2 );
			return false;
		}

		if ( Input.Down( "run" ) )
		{
			foreach ( var prop in trace.GameObject.GetAttachedGameObjects<Prop>() )
			{
				prop.Network.TakeOwnership();
			}
		}
		else
		{
			trace.GameObject.Network.TakeOwnership();
		}

		return true;
	}
}
