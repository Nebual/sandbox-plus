using Sandbox.UI.Tests;
using Sandbox.UI.Construct;

namespace Sandbox.UI
{
	[Library]
	public partial class MaterialSelector : Panel
	{
		VirtualScrollPanel Canvas;

		public MaterialSelector()
		{
			AddClass( "modelselector" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.AutoColumns = true;
			Canvas.Layout.ItemWidth = 64;
			Canvas.Layout.ItemHeight = 64;
			Canvas.OnCreateCell = async ( cell, data ) =>
			{
				var file = (string)data;
				Panel panel;
				if ( file.EndsWith( ".vmat" ) )
				{
					panel = AddLocalMaterialIcon( cell, file );
				}
				else
				{
					panel = await AddCloudMaterialIcon( cell, file );
					if ( panel == null )
					{
						return;
					}
				}
				panel.Tooltip = file; // this doesn't seem to work, appears to be broken Facepunch side. We can probably implement our own tooltip system eventually instead

				panel.AddEventListener( "onclick", () =>
				{
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					ConsoleSystem.Run( $"{currentTool}_material", file );
				} );
			};

			var spawnList = ModelSelector.GetSpawnList( "material" );

			foreach ( var file in spawnList )
			{
				if ( file.EndsWith( ".vmat" ) && !FileSystem.Mounted.FileExists( file + "_c" ) )
				{
					continue;
				}
				Canvas.AddItem( file );
			}
			// VirtualScrollPanel doesn't have a valid height (subsequent children overlap it within flex-direction: column) so calculate it manually
			Style.Height = (64 + 6) * (int)Math.Ceiling( spawnList.Count() / 5f );
		}

		private Panel AddLocalMaterialIcon( Panel cell, string file )
		{
			var material = Material.Load( file );

			var sceneWorld = new SceneWorld();
			var mod = new SceneObject( sceneWorld, Cloud.Model( "https://asset.party/drakefruit/cube32" ), Transform.Zero );
			mod.SetMaterialOverride( material );

			var sceneLight = new SceneLight( sceneWorld, Vector3.Up * 45.0f, 300.0f, Color.White * 30.0f );

			ScenePanel panel = cell.Add.ScenePanel( sceneWorld, Vector3.Up * 68, new Angles( 90, 0, 0 ).ToRotation(), 45, "icon" );
			panel.RenderOnce = true;
			return panel;
		}

		private async Task<Panel> AddCloudMaterialIcon( Panel cell, string file )
		{
			var package = await Package.FetchAsync( file, true, true );
			if ( package == null )
			{
				Log.Warning( $"MaterialSelector: Tried to load material package {file} - which was not found" );
				return null;
			}

			var panel = cell.Add.Image( package.Thumb, "icon" );
			return panel;
		}
	}
}
