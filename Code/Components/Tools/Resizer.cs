[Library( "tool_resizer", Title = "Resizer", Description = "Change the scale of things", Group = "construction" )]
public partial class ResizerTool : BaseTool
{
	public override bool Primary( SceneTraceResult trace )
	{
		return IncrementScale( trace, 1 );
	}
	public override bool Secondary( SceneTraceResult trace )
	{
		return IncrementScale( trace, -1 );
	}

	protected bool IncrementScale( SceneTraceResult trace, int resizeDir )
	{
		var go = trace.GameObject.Root;
		if ( !go.IsValid() )
			return false;

		var scale = new Vector3(
			MathX.Clamp( go.WorldScale.x + (0.5f * resizeDir), 0.4f, 4.0f ),
			MathX.Clamp( go.WorldScale.y + (0.5f * resizeDir), 0.4f, 4.0f ),
			MathX.Clamp( go.WorldScale.z + (0.5f * resizeDir), 0.4f, 4.0f )
		);
		var rescaled = Rescale( trace, scale );
		return rescaled && (Input.Pressed( "attack1" ) || Input.Pressed( "attack2" ));
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "reload" ) )
			return false;
		return Rescale( trace, Vector3.One );
	}

	protected bool Rescale( SceneTraceResult trace, Vector3 scale )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		var go = trace.GameObject.Root;
		if ( !go.IsValid() )
			return false;

		if ( !go.Components.TryGet<PropHelper>( out var propHelper ) )
			return false;

		if ( go.WorldScale != scale )
		{
			go.WorldScale = scale;

			if ( !propHelper.Rigidbody.IsValid() ) // && !propHelper.ModelPhysics.IsValid() ) // ragdolls probably can't be scaled yet, at least without recreating them like in https://github.com/Facepunch/sbox-scenestaging/commit/6aa3fe89335fece800414c8f1104ecb0c171b7d3
				return false;

			if ( propHelper.Rigidbody.IsValid() )
			{
				propHelper.Rigidbody.PhysicsBody.RebuildMass();
				propHelper.Rigidbody.PhysicsBody.Sleeping = false;
			}

			if ( propHelper.ModelPhysics.IsValid() )
			{
				propHelper.ModelPhysics.PhysicsGroup.RebuildMass();
				propHelper.ModelPhysics.PhysicsGroup.Sleeping = false;
			}

			foreach ( var child in go.Children )
			{
				if ( !child.IsValid() )
					continue;

				if ( !go.Components.TryGet<PropHelper>( out var childPropHelper ) )
					continue;

				if ( !childPropHelper.Rigidbody.IsValid() || !childPropHelper.ModelPhysics.IsValid() )
					continue;

				if ( childPropHelper.Rigidbody.IsValid() )
				{
					childPropHelper.Rigidbody.PhysicsBody.RebuildMass();
					childPropHelper.Rigidbody.PhysicsBody.Sleeping = false;
				}

				if ( childPropHelper.ModelPhysics.IsValid() )
				{
					childPropHelper.ModelPhysics.PhysicsGroup.RebuildMass();
					childPropHelper.ModelPhysics.PhysicsGroup.Sleeping = false;
				}
			}
			return true;
		}
		return false;
	}
}
