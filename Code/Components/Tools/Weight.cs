using Sandbox.UI;

[Library( "tool_weight", Title = "Weight", Description = "Change prop weight", Group = "construction" )]
public class WeightTool : BaseTool
{
	[Property, Range( 1f, 50000f, 1f ), Title( "Weight" )]
	public float TargetWeight { get; set; } = 100f;

	public static Dictionary<string, float> ModelWeights = new();

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
			trace.Body.Mass = TargetWeight;
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

	public void SetWeightConvar( float weight )
	{
		TargetWeight = weight;
		HintFeed.AddHint( "", $"Loaded weight of {weight}" );
	}
}
