using Sandbox;
using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities", Group = "construction" )]
	public partial class ColorTool : BaseTool
	{
		[ConVar.ClientData( "tool_color_color" )]
		public static string _ { get; set; } = "";

		public override void Simulate()
		{
			if ( !Game.IsServer )
				return;

			using ( Prediction.Off() )
			{
				var startPos = Owner.EyePosition;
				var dir = Owner.EyeRotation.Forward;

				if ( Input.Pressed( "attack1" ) )
				{
					var tr = DoTrace();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is not ModelEntity modelEnt )
						return;

					modelEnt.RenderColor = GetConvarValue( "tool_color_color" );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
				//prob awful way of doing it.
				if ( Input.Pressed( "attack2" ) )
				{
					var tr = DoTrace();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is not ModelEntity modelEnt )
						return;

					modelEnt.RenderColor = Color.White;

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
			}
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				var colorSelector = new ColorSelector();
				SpawnMenu.Instance?.ToolPanel?.AddChild( colorSelector );
			}
		}
	}
}


