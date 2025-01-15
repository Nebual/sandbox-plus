[Library( "tool_parent", Title = "Parent", Description = """
	Parent one or more objects to a singular object.
	NOTE: Parenting something *does not* weld them together.
	
	Primary Attack: Select one or more objects that 
	Reload: Clear all selected targets.
	Secondary Attack: Parent all selected objects to the target object.

	WIP - not yet. Sprint + Primary Attack: Find all objects that are constrained to the target, and target them all
	""", Group = "constraints" )]
public class Parent : BaseTool
{
	List<GameObject> targeted = new();

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		var go = trace.GameObject;

		if ( Input.Pressed( "attack1" ) && go.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			
			if ( targeted.Contains(go) )
			{
				RemoveTarget( go );
				return true;
			}

			// If we're holding Sprint, we want to find all constrained objects
			if ( Input.Down( "run" ) )
			{
				AddAllRecursively( go, propHelper );

				return true;
			}

			AddTarget( go );

			return true;
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		var go = trace.GameObject;

		if ( Input.Pressed( "attack2" ) && go.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			if ( targeted.Contains( go ) )
				RemoveTarget( go );

			foreach( var target in targeted )
			{
				target.SetParent( go );
			}

			UndoSystem.Add( Owner, PrepareUndo(targeted), go );

			ClearTargets();

			return true;
		}

		return false;
	}

	private Func<string> PrepareUndo(List<GameObject> objects)
	{
		List<GameObject> undoList = new();
		foreach(var obj in objects)
		{
			undoList.Add( obj );
		}


		return () =>
		{
			foreach(var obj in undoList)
			{
				Log.Info( obj.Root );
				obj.SetParent( null );
			}

			return "Undone Parenting " + undoList.Count + " objects together";
		};
	}

	private bool AddAllRecursively(GameObject target, PropHelper propHelper)
	{
		Log.Info( "Parent all!" );

		AddTarget( target );

		Log.Info( propHelper.Welds.Count );

		// TODO: Wait for https://github.com/Nebual/sandbox-plus/issues/57 and finish this afterwards.
		foreach ( var obj in propHelper.Welds )
		{
			Log.Info( obj.Body + " " + targeted.Contains( obj.Body ) );
			if ( !targeted.Contains( obj.Body ) && obj.Body.Components.TryGet<PropHelper>( out var objPH ) ) AddAllRecursively( obj.Body, objPH );
		}

		return true;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if (targeted.Count > 0)
			ClearTargets();

		return false;
	}

	//private Func<string> ReadyUndo( PropHelper propHelper, GameObject from)
	//{
	//	return () =>
	//	{
	//		propHelper.Unweld( from );

	//		return "Un-welded two objects";
	//	};
	//}

	private void AddTarget(GameObject obj)
	{
		targeted.Add( obj );
		AddGlow( obj );
	}

	private void RemoveTarget(GameObject obj)
	{
		targeted.Remove( obj );
		RemoveGlow( obj );
	}

	private void ClearTargets()
	{
		while(targeted.Count > 0)
			RemoveTarget( targeted.First() );
	}

	private void AddGlow(GameObject obj)
	{
		if ( obj.GetComponent<ModelRenderer>().IsValid() )
		{
			var glow = obj.GetOrAddComponent<HighlightOutline>();
			glow.Width = 0.25f;
			glow.Color = new Color( 100f, 100.0f, 100.0f, 1.0f );

			foreach ( var child in obj.Children )
			{
				if ( !child.GetComponent<ModelRenderer>().IsValid() )
					continue;

				glow = child.GetOrAddComponent<HighlightOutline>();
				glow.Color = new Color( 0.1f, 1.0f, 1.0f, 1.0f );
			}
		}
	}

	private void RemoveGlow( GameObject obj )
	{
		if ( obj.IsValid() )
		{
			foreach ( var child in obj.Children )
			{
				if ( !child.Components.Get<ModelRenderer>().IsValid() )
					continue;

				if ( child.Components.TryGet<HighlightOutline>( out var childglow ) )
				{
					childglow.Destroy();
				}
			}

			if ( obj.Components.TryGet<HighlightOutline>( out var glow ) )
			{
				glow.Destroy();
			}
		}
	}

	private void RemoveAllGlows()
	{
		foreach (var obj in targeted)
		{
			RemoveGlow( obj );
		}
	}
}
