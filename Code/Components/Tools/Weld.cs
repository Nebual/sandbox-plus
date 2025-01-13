[Library( "tool_weld", Title = "Weld", Description = "Weld stuff together", Group = "constraints" )]
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

			UndoSystem.Add( creator: this.Owner, callback: ReadyUndo( propHelper, welded ), prop: propHelper.GameObject);

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
			propHelper.RemoveConstraints( ConstraintType.Weld );

			welded = null;
			return true;
		}

		return false;
	}

	private Func<string> ReadyUndo( PropHelper propHelper, GameObject from)
	{
		return () =>
		{
			if ( propHelper.IsValid() )
				propHelper.RemoveConstraints( ConstraintType.Weld, from );

			return "Un-welded two objects";
		};
	}
}
