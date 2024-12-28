[Library( "tool_resizer", Title = "Resizer", Description = "Change the scale of things", Group = "construction" )]
public partial class ResizerTool : BaseTool
{
	public override bool Primary( SceneTraceResult trace )
	{
		return IncrementScale( trace, 1 );
	}
	public override bool Secondary( SceneTraceResult trace )
	{
		return IncrementScale( trace, -1 );
	}

	protected bool IncrementScale( SceneTraceResult trace, int resizeDir )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		var skinnedModelRenderer = trace.GameObject.GetComponent<SkinnedModelRenderer>();
		if ( skinnedModelRenderer.IsValid() )
		{
			var go = skinnedModelRenderer.GetBoneObject( trace.Bone );
			var size = go.WorldScale + (resizeDir * 0.5f * Time.Delta);

			SetRagSize( trace.GameObject, size, trace.Bone );
		}
		else
		{
			var go = trace.GameObject;
			var size = go.WorldScale + (resizeDir * 0.5f * Time.Delta);

			SetPropSize( trace.GameObject, size );
		}

		if ( Input.Pressed( "attack1" ) || Input.Pressed( "attack2" ) )
		{
			return true; // default ToolEffects
		}
		else
		{
			Parent.ToolEffects( trace.EndPosition, trace.Normal, true );
			return false;
		}
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "reload" ) )
			return false;

		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		var skinnedModelRenderer = trace.GameObject.GetComponent<SkinnedModelRenderer>();
		if ( skinnedModelRenderer.IsValid() )
		{
			SetRagSize( trace.GameObject, Vector3.One, trace.Bone );
		}
		else
		{
			SetPropSize( trace.GameObject, Vector3.One );
		}
		return true;
	}

	[Rpc.Broadcast]
	void SetPropSize( GameObject gameObject, Vector3 size )
	{
		gameObject.WorldScale = size;
	}

	[Rpc.Broadcast]
	void SetRagSize( GameObject gameObject, Vector3 size, int index )
	{
		var skinnedModelRenderer = gameObject.GetComponent<SkinnedModelRenderer>();

		if ( !skinnedModelRenderer.IsValid() )
			return;

		var go = skinnedModelRenderer.GetBoneObject( index );

		go.Flags = GameObjectFlags.ProceduralBone;
		go.WorldScale = size;
	}
}
