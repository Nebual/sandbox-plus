using Sandbox.UI;
using Sandbox.UI.Construct;

public partial class HintFeed : Panel
{
	static HintFeed Current;

	public HintFeed()
	{
		StyleSheet.Load( "ui/HintFeed/HintFeed.scss" );
		Current = this;
	}

	[ClientRpc]
	public static void AddHint( string icon, string msg )
	{
		var e = Current.AddChild<HintFeedEntry>();

		var iconClasses = GetIconClasses( icon );

		if ( !string.IsNullOrEmpty( icon ) )
		{
			e.Icon = e.Add.Icon( icon, iconClasses );
		}
		e.Message = e.Add.Label( msg, "msg" );
	}

	private static string GetIconClasses( string icon )
	{
		List<string> classes = new() {
			"icon",
			icon
		};
		return string.Join( " ", classes );
	}
}
