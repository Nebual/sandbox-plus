public class StackerPreviewModels
{
	public Stacker.StackerTool Stacker { get; set; }
	private List<GameObject> previews = new List<GameObject>();
	private int previousCount = -1;
	private Model currentModel;

	public StackerPreviewModels() {
		if ( previews == null ) previews = new List<GameObject>();
	}

	public void Update( SceneTraceResult trace, List<Stacker.StackPropData> propData )
	{
		previews.RemoveAll( item => !item.IsValid() );

		if ( previews.Count == 0 )
		{
			currentModel = Model.Load( trace.GameObject.GetComponent<ModelRenderer>().Model.Name );

			for ( var i = 0; i < propData.Count; i++ )
			{
				previews.Add( CreateObj( propData[i], trace.GameObject) );
			}
		}
		else
		{
			// Remove any excess (the count went down). Doesn't need an if, as a `while` evaluates first
			while(previews.Count > propData.Count)
			{
				DeleteAt( propData.Count );
			}

			if (previews.Count < propData.Count)
			{
				for(var i = previews.Count; i < propData.Count; i++)
				{
					previews.Add( CreateObj( propData[i], trace.GameObject ) );
				}
			}

			foreach ( var (obj, index) in previews.Select( ( value, index ) => (value, index) ) )
			{
				obj.WorldPosition = propData[index].Position;
				obj.WorldRotation = propData[index].Rotation;
			}

			SetEnabled( true );
			previousCount = propData.Count;
		}
	}

	private GameObject CreateObj( Stacker.StackPropData data, GameObject parent)
	{
		var previewObject = new GameObject();
		previewObject.WorldPosition = data.Position;
		previewObject.WorldRotation = data.Rotation;
		previewObject.Parent = parent;

		var renderer = previewObject.Components.Create<ModelRenderer>();
		renderer.Tint = Color.White.WithAlpha( 0.5f );
		renderer.Model = currentModel;

		previewObject.NetworkSpawn();

		return previewObject;
	}

	public void Destroy()
	{
		DeleteAll();
		currentModel = null;
	}

	private void DeleteAll()
	{
		while ( previews.Any() )
		{
			DeleteAt( 0 );
		}
	}

	private void DeleteAt(int i)
	{
		previews.GetRange( i, 1 ).ForEach( ( GameObject obj ) => obj.Destroy() );
		previews.RemoveAt( 0 );
	}

	public void SetEnabled( bool enabled )
	{
		if ( previews.Count == 0 )
			return;

		if ( enabled )
		{

		}
		else
		{
			DeleteAll();
			currentModel = null;
		}

	}

	//public void SetTint( Color tint )
	//{
	//	if ( !previewObject.IsValid() )
	//		return;
	//	var modelRenderer = previewObject.GetComponent<ModelRenderer>();
	//	if ( !modelRenderer.IsValid() )
	//		return;

	//	modelRenderer.Tint = tint.WithAlpha( modelRenderer.Tint.a );
	//}
}
