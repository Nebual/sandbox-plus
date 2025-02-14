public partial class Player
{
	private GameObject LastTapped;
	private int CurrentIndex = -1;

	[ConCmd( "sbox_tool_auto" )]
	public static void SboxToolAutoCmd( string param1 = null )
	{
		Player.FindLocalPlayer().SboxToolAuto( param1 == "debug" );
	}
	public void SboxToolAuto( bool debug = false )
	{
		var tr = DoBasicTrace();
		if ( !tr.GameObject.IsValid() || tr.GameObject.IsWorld() ) return;

		List<string> toolOptionsP1 = new();
		List<string> toolOptions = new();
		List<string> componentNames = new();

		foreach ( var component in tr.GameObject.Components.GetAll() )
		{
			// Components can define their own custom tools via:
			// public string[] SboxToolAutoTools => new string[] { "tool_constraint" };
			if ( TypeLibrary.GetPropertyValue( component, "SboxToolAutoTools" ) is IEnumerable<string> customToolsEnumerable )
			{
				foreach ( var customTool in customToolsEnumerable )
				{
					if ( TypeLibrary.GetType( customTool ) != null )
					{
						toolOptionsP1.Add( customTool );
					}
				}
			}
			var className = TypeLibrary.GetType( component.GetType() ).ClassName;
			// Generic entity -> tool handler
			if ( className.StartsWith( "ent_" ) )
			{
				var possibleTool = className.Replace( "ent_", "tool_" );
				if ( TypeLibrary.GetType( possibleTool ) != null )
				{
					// nice, there's a tool whose ClassName aligns with the entity it spawns (eg. ent_wirebutton + tool_wirebutton)
					toolOptions.Add( possibleTool );
				}
			}
			componentNames.Add( className );
		}

		if ( debug )
		{
			Log.Info( $"sbox_tool_auto debug: tr.Entity: {tr.GameObject} Name: {tr.GameObject.Name} Components: {string.Concat( componentNames, ", " )}" );
			foreach ( var property in TypeLibrary.GetPropertyDescriptions( tr.GameObject, true ) )
			{
				Log.Info( $"sbox_tool_auto debug: tr.Entity has Property: {property.Name}" );
			}
		}

		// Wirebox compatible entity handler
		if ( tr.GameObject.GetComponent<IWireComponent>() is not null )
		{
			toolOptions.Add( "tool_wiring" );
			toolOptions.Add( "tool_debugger" );
		}

		if ( tr.GameObject.GetComponent<Prop>() is not null )
		{
			toolOptions.Add( "tool_constraint" );
			toolOptions.Add( "physgun" );
		}

		toolOptions = toolOptionsP1.Concat( toolOptions ).Distinct().ToList();

		if ( toolOptions.Count == 0 ) return;

		// Always start with the first tool when picking a new Entity
		if ( tr.GameObject != LastTapped )
		{
			LastTapped = tr.GameObject;
			CurrentIndex = -1;
		}
		CurrentIndex += 1;
		if ( CurrentIndex >= toolOptions.Count ) CurrentIndex = 0;
		var tool = toolOptions[CurrentIndex];

		if ( tool.StartsWith( "tool_" ) )
		{
			ConsoleSystem.SetValue( "tool_current", tool );
			ConsoleSystem.Run( "weapon_switch", "weapon_tool" );
		}
		else
		{
			ConsoleSystem.Run( "weapon_switch", tool );
		}
	}
}
