[Library( "tool_weld", Description = "Weld stuff together", Group = "construction" )]
public class Weld : BaseTool
{
	GameObject welded;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "attack1" ) && trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			if ( welded == null )
			{
				welded = trace.GameObject;
				return true;
			}

			if ( trace.GameObject == welded )
				return false;

			propHelper.Weld( welded );

			UndoSystem.Add( this.Owner, ReadyUndo( propHelper, welded ));

			welded = null;
			return true;
		}

		return false;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "reload" ) && trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			propHelper.Unweld();

			welded = null;
			return true;
		}

		return false;
	}

	private Func<string> ReadyUndo( PropHelper propHelper, GameObject from)
	{
		return () =>
		{
			propHelper.Unweld( from );

			return "Un-welded two objects";
		};
	}
}
