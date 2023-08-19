using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_whatisthat", Title = "What is that?", Description = "Prop identificationifier (prints to console because what is chat?)", Group = "construction" )]
	public partial class WhatIsThatTool : BaseTool
	{
		public override void Simulate()
		{
			if ( !Game.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( "attack1" ) )
				{
					var tr = DoTrace();

					if ( !tr.Hit )
						return;

					if ( !tr.Entity.IsValid() )
						return;

					var attached = !tr.Entity.IsWorld && tr.Body.IsValid() && tr.Body.PhysicsGroup != null && tr.Body.GetEntity().IsValid();

					if ( attached && tr.Entity is not Prop )
						return;

					CreateHitEffects( tr.EndPosition, tr.Normal );
					var message = $"That is a: {tr.Entity.ClassName} ({tr.Entity.NetworkIdent})";
					var prop = tr.Entity as Prop;
					if ( prop.IsValid() )
					{
						message += $" {prop.Model.Name}";
						if ( prop.PhysicsBody.IsValid() )
						{
							message += $" weighing {prop.PhysicsBody.Mass}";
						}
						if ( prop.GetPlayerOwner().IsValid() )
						{
							message += $" owned by {prop.GetPlayerOwner()}";
						}
					}
					// todo: how do print to chat/etc?
					Log.Info( message );
				}
			}
		}
	}
}
