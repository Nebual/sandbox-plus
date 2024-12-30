[Library( "model_skin", Title = "Model Skin Changer", Description = "Cycles through the models skins", Group = "rendering" )]
public partial class ModelSkinTool : BaseTool
{
	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() )
				return false;

			if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
				return false;

			if ( propHelper.Prop.Model.MaterialGroupCount == 0 )
			{
				return false;
			}
			else
			{
				var currentGroup = propHelper.Prop.Model.GetMaterialGroupIndex( propHelper.Prop.MaterialGroup );
				var nextGroup = currentGroup + 1;

				if ( nextGroup >= propHelper.Prop.Model.MaterialGroupCount )
				{
					nextGroup = 0;
				}

				BroadcastMaterialGroup( propHelper.Prop, propHelper.Prop.Model.GetMaterialGroupName( nextGroup ) );
			}

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	private static void BroadcastMaterialGroup( Prop prop, string materialGroup )
	{
		prop.MaterialGroup = materialGroup;
	}
}
