using Sandbox.UI.Construct;
using Sandbox.UI.Tests;

namespace Sandbox.UI
{
	[Library]
	public partial class ColorSelector : Panel
	{
		VirtualScrollPanel Canvas;

		public ColorSelector()
		{
			AddClass( "modelselector" );
			AddClass( "active" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.AutoColumns = true;
			Canvas.Layout.ItemWidth = 64;
			Canvas.Layout.ItemHeight = 64;

			var colors = new Color[] { Color.White, Color.Black, Color.Red, Color.Cyan, Color.Green, Color.Magenta, Color.Yellow, Color.Blue, Color.Gray, Color.Orange };
			
			int index = 0;

			Canvas.OnCreateCell = ( cell, data ) =>
			{
				var sceneWorld = new SceneWorld();
				var mod = new SceneObject( sceneWorld, Cloud.Model( "https://asset.party/drakefruit.cube32" ), Transform.Zero );
				var color = colors[index];
				mod.ColorTint = color;

				var sceneLight = new SceneLight( sceneWorld, Vector3.Up * 45.0f, 300.0f, Color.White * 30.0f );

				ScenePanel panel = cell.Add.ScenePanel( sceneWorld, Vector3.Up * 68, new Angles( 90, 0, 0 ).ToRotation(), 45, "icon" );
				panel.RenderOnce = true;

				panel.AddEventListener( "onclick", () =>
				{
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					ConsoleSystem.Run( $"{currentTool}_color", color );
				} );

				if ( index < colors.Length )
				{
					index++;
				}
			};

			foreach (var color in colors)
			{
				Canvas.AddItem( color );
			}
		}
	}
}
