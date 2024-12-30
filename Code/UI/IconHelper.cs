namespace Sandbox.UI;

public static class IconHelper
{
	public static string GetFileIcon( string filePath )
	{
		if ( filePath.EndsWith( ".cs" ) )
		{
			return "📄";
		}
		else if ( filePath.EndsWith( ".json" ) || filePath.EndsWith( ".config" ) )
		{
			return "📋";
		}
		else if ( filePath.EndsWith( ".png" ) || filePath.EndsWith( ".jpg" ) || filePath.EndsWith( ".svg" ) || filePath.EndsWith( ".vtex_c" ) )
		{
			return "🖼️";
		}
		else if ( filePath.EndsWith( ".scss" ) )
		{
			return "🎨";
		}
		else if ( filePath.EndsWith( ".ttf" ) )
		{
			return "🔤";
		}
		else if ( filePath.EndsWith( ".vsnd_c" ) )
		{
			return "🔊";
		}
		else if ( filePath.EndsWith( ".sound_c" ) )
		{
			return "🎶";
		}
		else if ( filePath.EndsWith( ".sndscape_c" ) )
		{
			return "🎼";
		}
		// else if ( filePath.EndsWith( ".vtex_c" ) )
		// {
		//	 return "🔳";
		// }
		else if ( filePath.EndsWith( ".vmat_c" ) )
		{
			return "🌐";
		}
		else if ( filePath.EndsWith( ".vmdl_c" ) )
		{
			return "🧊";
		}
		else if ( filePath.EndsWith( ".vpcf_c" ) )
		{
			return "✨";
		}
		else if ( filePath.EndsWith( ".scene_c" ) )
		{
			return "🌄";
		}
		else if ( filePath.EndsWith( ".prefab_c" ) )
		{
			return "📦";
		}
		else if ( filePath.EndsWith( "_c" ) )
		{
			return "💎";
		}

		return "❓";
	}
}
