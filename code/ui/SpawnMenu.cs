
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[Library]
public partial class SpawnMenu : Panel
{
	public static SpawnMenu Instance;

	public SpawnMenu()
	{
		Instance = this;

		StyleSheet.Load( "/ui/SpawnMenu.scss" );

		var left = Add.Panel( "left" );
		{
			var tabs = left.AddChild<ButtonGroup>();
			tabs.AddClass( "tabs" );

			var body = left.Add.Panel( "body" );

			{
				var props = body.AddChild<SpawnList>();
				tabs.SelectedButton = tabs.AddButtonActive( "Props", ( b ) => props.SetClass( "active", b ) );

				var ents = body.AddChild<EntityList>();
				tabs.AddButtonActive( "Entities", ( b ) => ents.SetClass( "active", b ) );
			}
		}

		var right = Add.Panel( "right" );
		{
			var tabs = right.Add.Panel( "tabs" );
			{
				tabs.Add.Button( "Tools" ).AddClass( "active" );
				tabs.Add.Button( "Utility" );
			}
			var body = right.Add.Panel( "body" );
			{
				var list = body.Add.Panel( "toollist" );
				{
					foreach ( var entry in Library.GetAllAttributes<Sandbox.Tools.BaseTool>() )
					{
						if ( entry.Title.StartsWith( "Base" ) )
							continue;

						var button = list.Add.Button( entry.Title );
						button.SetClass( "active", entry.Name == ConsoleSystem.GetValue( "tool_current" ) );

						button.AddEvent( "onclick", () =>
						{
							ConsoleSystem.Run( "tool_current", entry.Name );
							ConsoleSystem.Run( "inventory_current", "weapon_tool" );

							foreach ( var child in list.Children )
								child.SetClass( "active", child == button );
						} );
					}
				}
				body.Add.Panel( "inspector" );
			}
		}

	}

	public override void Tick()
	{
		base.Tick();

		Parent.SetClass( "spawnmenuopen", Input.Down( InputButton.Menu ) );
	}

}
