public class PreviewModel
{
	public string ModelPath { get; set; }
	public Vector3 PositionOffset { get; set; }
	public Rotation RotationOffset { get; set; }
	public float NormalOffset { get; set; }
	public bool FaceNormal { get; set; }

	public GameObject previewObject;

	public void Update( SceneTraceResult trace )
	{
		if ( !previewObject.IsValid() )
		{
			previewObject = new GameObject();

			var renderer = previewObject.Components.Create<ModelRenderer>();
			renderer.Tint = Color.White.WithAlpha( 0.5f );
			renderer.Model = Model.Load( ModelPath );

			previewObject.NetworkSpawn();
		}

		previewObject.WorldPosition = trace.HitPosition + PositionOffset + trace.Normal * NormalOffset;
		previewObject.WorldRotation = (FaceNormal ? Rotation.LookAt( trace.Normal, trace.Direction ) : Rotation.Identity) * RotationOffset;
		SetEnabled( true );
	}

	public void Destroy()
	{
		previewObject?.Destroy();
	}

	public void SetEnabled( bool enabled )
	{
		if ( !previewObject.IsValid() )
			return;
		var modelRenderer = previewObject.GetComponent<ModelRenderer>();
		if ( !modelRenderer.IsValid() )
			return;

		modelRenderer.Tint = modelRenderer.Tint.WithAlpha( enabled ? 0.5f : 0 );
	}

	public void SetTint( Color tint )
	{
		if ( !previewObject.IsValid() )
			return;
		var modelRenderer = previewObject.GetComponent<ModelRenderer>();
		if ( !modelRenderer.IsValid() )
			return;

		modelRenderer.Tint = tint.WithAlpha( modelRenderer.Tint.a );
	}
}
