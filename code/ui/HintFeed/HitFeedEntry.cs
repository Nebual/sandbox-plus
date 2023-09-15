using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;


public partial class HintFeedEntry : Panel
{
	public IconPanel Icon { get; internal set; }
	public Label Message { get; internal set; }

	public RealTimeSince TimeSinceBorn = 0;

	public HintFeedEntry() { }

	public override void Tick()
	{
		base.Tick();

		if ( TimeSinceBorn > 3 )
		{
			Delete();
		}
	}
}
