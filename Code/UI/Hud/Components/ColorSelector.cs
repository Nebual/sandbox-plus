using Sandbox.UI.Construct;
using Sandbox.UI.Tests;

namespace Sandbox.UI
{
	[Library]
	public partial class ColorSelector : Panel
	{
		public Action<Color> OnValueChanged { get; set; }
		protected Color Value;
		public SerializedProperty SerializedProperty
		{
			get => _property;
			set
			{
				_property = value;
				Value = _property.GetValue<Color>();
			}
		}
		SerializedProperty _property;
		VirtualScrollPanel Canvas;

		private bool initialized;
		protected override void OnParametersSet()
		{
			if ( initialized ) return;
			initialized = true;
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
				if ( index < 0 || index >= colors.Length )
				{
					return;
				}
				var sceneWorld = new SceneWorld();
				var mod = new SceneObject( sceneWorld, Cloud.Model( "https://asset.party/drakefruit.cube32" ), Transform.Zero );
				var color = colors[index];
				mod.ColorTint = color;

				var sceneLight = new SceneLight( sceneWorld, Vector3.Up * 65.0f, 100.0f, Color.White * 5.0f );

				ScenePanel panel = cell.Add.ScenePanel( sceneWorld, Vector3.Up * 68, new Angles( 90, 0, 0 ).ToRotation(), 45, "icon" );
				panel.RenderOnce = true;

				panel.AddEventListener( "onclick", () =>
				{
					Value = color;
					OnValueChanged?.Invoke( Value );
					_property?.SetValue( Value );
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
			// VirtualScrollPanel doesn't have a valid height (subsequent children overlap it within flex-direction: column) so calculate it manually
			Style.Height = (64 + 6) * (int)Math.Ceiling( colors.Length / 5f );
		}
	}
}
