[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities", Group = "rendering" )]
public partial class ColorTool : BaseTool
{
	[Property, Title( "Color" )]
	public Color TargetColor { get; set; } = Color.Blue;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) ) return false;
		
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
			return false;

		BroadcastColor( propHelper, TargetColor );

		return true;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack2" ) ) return false;
		
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
			return false;

		BroadcastColor( propHelper, Color.White );

		return true;
	}

	[Rpc.Broadcast]
	private static void BroadcastColor( PropHelper propHelper, Color color )
	{
		propHelper.Prop.Tint = color;
	}
}
