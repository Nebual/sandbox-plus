using Sandbox.UI;

[Library( "tool_weight", Title = "Weight", Description = "Change prop weight", Group = "construction" )]
public class WeightTool : BaseTool
{
	[ConVar( "tool_weight_weight" )]
	public static float _ { get; set; } = 100f;

	public static Dictionary<string, float> ModelWeights = new();

	private static Slider WeightSlider;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			var prop = trace.GameObject.GetComponent<Prop>();
			if ( !ModelWeights.ContainsKey( prop.Model.Name ) )
			{
				ModelWeights.Add( prop.Model.Name, trace.Body.Mass );
			}
			trace.Body.Mass = float.Parse( GetConvarValue( "tool_weight_weight" ) );
			return true;
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
			return false;

		if ( Input.Pressed( "attack2" ) )
		{
			SetWeightConvar( trace.Body.Mass );
			return true;
		}

		return false;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
			return false;

		if ( Input.Pressed( "reload" ) )
		{
			var prop = trace.GameObject.GetComponent<Prop>();
			if ( ModelWeights.ContainsKey( prop.Model.Name ) )
			{
				trace.Body.Mass = ModelWeights[prop.Model.Name];
			}
			else
			{
				trace.Body.Mass = 100f;
			}
			return true;
		}

		return false;
	}

	[Rpc.Owner]
	public void SetWeightConvar( float weight )
	{
		ConsoleSystem.Run( "tool_weight_weight", weight );
		if ( WeightSlider.IsValid() )
		{
			WeightSlider.Value = weight;
		}
		OnWeightConvarChanged( weight );
		HintFeed.AddHint( "", $"Loaded weight of {weight}" );
	}
	public void OnWeightConvarChanged( float weight )
	{
		Description = $"Set weight to {weight}";
	}

	public override void CreateToolPanel()
	{
		WeightSlider = new Slider
		{
			Label = "Weight",
			Min = 1f,
			Max = 1000f,
			Step = 1f,
			Convar = "tool_weight_weight",
			OnValueChanged = OnWeightConvarChanged,
		};
		SpawnMenu.Instance?.ToolPanel?.AddChild( WeightSlider );
	}
}
