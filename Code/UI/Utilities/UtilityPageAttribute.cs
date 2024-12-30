namespace Scenebox;

public class UtilityPageAttribute : System.Attribute
{
	public string Title { get; }
	public string Group { get; }

	public UtilityPageAttribute( string title, string group = "" )
	{
		Title = title;
		Group = group;
	}
}
