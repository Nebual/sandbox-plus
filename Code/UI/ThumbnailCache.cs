namespace Sandbox.UI;

public static class ThumbnailCache
{
	static Dictionary<Model, Texture> cache = new();
	static List<Model> queue = new();

	public static void Clear()
	{
		cache.Clear();
	}

	public static Texture Get( Model model )
	{
		if ( cache.TryGetValue( model, out var tex ) )
			return tex;

		if ( !queue.Contains( model ) )
		{
			queue.Add( model );
		}

		return Texture.Transparent;
	}

	public static void CheckTextureQueue()
	{
		if ( queue.Count > 0 )
		{
			var model = queue[0];
			queue.RemoveAt( 0 );
			GenerateTexture( model );
		}
	}

	public static void GenerateTexture( Model model )
	{
		if ( model is null || model.IsError )
		{
			cache[model] = Texture.Invalid;
			return;
		}

		var sceneWorld = new SceneWorld();
		var sceneModel = new SceneModel( sceneWorld, model, new() );
		sceneModel.Rotation = Rotation.From( 0, 180, 0 );
		var bounds = sceneModel.LocalBounds;
		var center = bounds.Center;

		var sceneCamera = new SceneCamera();
		sceneCamera.World = sceneWorld;
		sceneCamera.Rotation = Rotation.From( 25, 220, 0 );
		sceneCamera.FieldOfView = 30.0f;
		var distance = MathX.SphereCameraDistance( bounds.Size.Length * 0.4f, sceneCamera.FieldOfView );
		sceneCamera.Position = center + sceneCamera.Rotation.Backward * distance;
		var right = sceneCamera.Rotation.Right;

		var sceneLight = new SceneLight( sceneWorld, sceneCamera.Position + Vector3.Up * 500.0f + right * 100.0f, 1000.0f, new Color( 1.0f, 0.9f, 0.9f ) * 50.0f );
		var sceneCubemap = new SceneCubemap( sceneWorld, Texture.Load( "textures/cubemaps/default2.vtex" ), BBox.FromPositionAndSize( Vector3.Zero, 1000 ) );

		var texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
		Graphics.RenderToTexture( sceneCamera, texture );
		cache[model] = texture;

		sceneCubemap.Delete();
		sceneLight.Delete();
		sceneCamera.Dispose();
		sceneModel.Delete();
		sceneWorld.Delete();
	}
}
