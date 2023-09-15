namespace Sandbox.Tools
{
	[Library( "tool_whatisthat", Title = "What is that?", Description = "Prop identificationifier", Group = "construction" )]
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
						message += $" {prop.Model.Name},\n";
						if ( prop.PhysicsBody.IsValid() )
						{
							message += $" weighing {prop.PhysicsBody.Mass},";
						}

						var playerOwner = prop.GetPlayerOwner();
						if ( playerOwner.IsValid() )
						{
							var ownerClient = Game.Clients.FirstOrDefault( c => c.NetworkIdent == (playerOwner?.Owner?.NetworkIdent ?? playerOwner?.NetworkIdent) );
							message += $" owned by {ownerClient?.Name ?? playerOwner.ToString()},";
						}
					}
					message += $" trace position {tr.EndPosition}";

					LogClientside( To.Single( Owner.Client ), message.Replace( "\n", "" ) );
					HintFeed.AddHint( To.Single( Owner.Client ), "question_mark", message );
				}
			}
		}

		[ClientRpc]
		public static void LogClientside( string message )
		{
			Log.Info( message );
		}
	}
}
