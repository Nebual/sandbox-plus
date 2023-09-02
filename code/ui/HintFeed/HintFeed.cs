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
	public static void AddHint( string msg, bool redo = false )
	{
		var e = Current.AddChild<HintFeedEntry>();

		var undoRedo = redo ? "redo" : "undo";
		e.Icon = e.Add.Icon(undoRedo, $"icon {undoRedo}");
		e.Name = e.Add.Label( msg, "msg" );
	}
}
