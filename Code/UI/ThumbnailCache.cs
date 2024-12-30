using System.Threading.Tasks;

namespace Scenebox;

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
		var sceneCamera = new SceneCamera();
		var sceneLight = new SceneDirectionalLight( sceneWorld, Rotation.From( 45, 45, 0 ), Color.White );
		sceneCamera.World = sceneWorld;
		sceneCamera.Rotation = Rotation.From( 25, -45, 0 );
		sceneModel.Rotation = Rotation.From( 0, 180, 0 );

		var bounds = sceneModel.Bounds;
		var center = bounds.Center;
		var distance = bounds.Size.Length * 0.8f;
		sceneCamera.Position = center + sceneCamera.Rotation.Backward * distance;

		var texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
		Graphics.RenderToTexture( sceneCamera, texture );
		cache[model] = texture;

		sceneLight.Delete();
		sceneCamera.Dispose();
		sceneModel.Delete();
		sceneWorld.Delete();
	}
}
