using Sandbox;
using Sandbox.Tools;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Xml.Linq;

public partial class HintFeed : Panel
{
	static HintFeed Current;

	public HintFeed()
	{
		StyleSheet.Load( "ui/HintFeed/HintFeed.scss" );
		Current = this;
	}

	[ClientRpc]
	public static void AddHint(string type, string msg )
	{
		var e = Current.AddChild<HintFeedEntry>();

		var iconName = GetIconName( type );
		var iconClasses = GetIconClasses( type );

		if ( !string.IsNullOrEmpty( iconName ) )
		{
			e.Icon = e.Add.Icon( iconName, iconClasses );
		}
		e.Message = e.Add.Label( msg, "msg" );
	}

	private static string GetIconName( string type )
	{
		string name = type switch
		{
			"undo" => type,
			"redo" => type,
			"whatis" => "question_mark",
			_ => null,
		};
		return name;
	}

	private static string GetIconClasses(string type )
	{
		List<string> classes = new List<string> { "icon" };

		if (type == "undo" || type == "redo")
		{
			classes.Add( type );
		}
		else if (type == "whatis")
		{
			classes.Add( "question" );
		}
		return string.Join(" ", classes);
	}
}
