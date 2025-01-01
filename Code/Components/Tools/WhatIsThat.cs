using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_whatisthat", Title = "What is that?", Description = "Prop identificationifier", Group = "rendering" )]
	public partial class WhatIsThatTool : BaseTool
	{
		public override bool Primary( SceneTraceResult tr )
		{
			if ( !Input.Pressed( "attack1" ) ) return false;

			if ( !tr.Hit || !tr.GameObject.IsValid() )
				return false;

			var message = $"That is a: [{string.Join( ", ", ListTypes( tr.GameObject ) )}],";
			var prop = tr.GameObject.GetComponent<PropHelper>();
			if ( prop.IsValid() )
			{
				Clipboard.SetText(prop.Prop.Model.Name);
				message += $" {prop.Prop.Model.Name},\n";
				if ( tr.Body.IsValid() )
				{
					message += $" weighing {tr.Body.Mass:F2},";
				}

				var playerOwner = tr.GameObject?.Root?.GetComponent<Player>();
				if ( playerOwner.IsValid() )
				{
					message += $" owned by {playerOwner.Name ?? playerOwner.ToString()},";
				}
			}
			message += $" trace pos ({tr.EndPosition})";

			Log.Info( message.Replace( "\n", "" ) );
			HintFeed.AddHint( "question_mark", message, 6 );

			return true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var trace = Parent.BasicTraceTool();

			DoAxisOverlay( trace, Scene.Camera );
		}

		public static string[] ListTypes( GameObject go )
		{
			var types = new List<string>();
			foreach ( var component in go.Components.GetAll() )
			{
				types.Add( TypeLibrary.GetType( component.GetType() ).ClassName );
			}
			return types.ToArray();
		}
	}
}
