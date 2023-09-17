using Sandbox;
using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_weight", Title = "Weight", Description = "Change prop weight", Group = "construction" )]
	public partial class WeightTool : BaseTool
	{
		[ConVar.ClientData( "tool_weight_weight" )]
		public static float _ { get; set; } = 100f;

		public static Dictionary<string, float> ModelWeights = new();

		private static Slider WeightSlider;

		public override void Simulate()
		{
			if ( !Game.IsServer )
				return;

			using ( Prediction.Off() )
			{
				var tr = DoTrace();

				if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld )
					return;

				if ( tr.Entity is not ModelEntity modelEnt || !modelEnt.PhysicsBody.IsValid() )
					return;

				if ( Input.Pressed( "attack1" ) )
				{
					if ( !ModelWeights.ContainsKey( modelEnt.GetModelName() ) )
					{
						ModelWeights.Add( modelEnt.GetModelName(), modelEnt.PhysicsBody.Mass );
					}
					modelEnt.PhysicsBody.Mass = float.Parse( GetConvarValue( "tool_weight_weight" ) );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
				if ( Input.Pressed( "attack2" ) )
				{
					SetWeightConvar( To.Single( Owner ), modelEnt.PhysicsBody.Mass );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
				if ( Input.Pressed( "reload" ) )
				{
					if ( ModelWeights.ContainsKey( modelEnt.GetModelName() ) )
					{
						modelEnt.PhysicsBody.Mass = ModelWeights[modelEnt.GetModelName()];

						CreateHitEffects( tr.EndPosition, tr.Normal );
					}
				}
			}
		}

		[ClientRpc]
		public static void SetWeightConvar( float weight )
		{
			ConsoleSystem.Run( $"tool_weight_weight", weight );
			if ( WeightSlider.IsValid() )
				WeightSlider.Value = weight;
			HintFeed.AddHint( "", $"Loaded weight of {weight}" );
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				WeightSlider = new Slider
				{
					Label = "Weight",
					Min = 1f,
					Max = 1000f,
					Step = 1f,
					Convar = "tool_weight_weight"
				};
				SpawnMenu.Instance?.ToolPanel?.AddChild( WeightSlider );
			}
		}
	}
}


