[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities", Group = "construction" )]
public partial class ColorTool : BaseTool
{
	[ConVar( "tool_color_color" )]
	public static string _ { get; set; } = "";

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) ) return false;
		
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
			return false;

		BroadcastColor( propHelper, GetConvarValue( "tool_color_color" ) );

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

	public override void CreateToolPanel()
	{
		var colorSelector = new Sandbox.UI.ColorSelector();
		SpawnMenu.Instance?.ToolPanel?.AddChild( colorSelector );
	}
}
