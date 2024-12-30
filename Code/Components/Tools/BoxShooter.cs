[Library( "tool_boxgun", Title = "Box Gun", Description = "Shoot boxes", Group = "fun" )]
public class BoxShooter : BaseTool
{
	RealTimeSince timeSinceShoot;

	string modelToShoot = "models/citizen_props/crate01.vmdl";

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			ShootBox();
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( timeSinceShoot > 0.05f )
		{
			timeSinceShoot = 0;

			ShootBox();
		}

		return false;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "reload" ) && trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) && !string.IsNullOrEmpty( propHelper.Prop.Model.Name ) )
		{
			modelToShoot = propHelper.Prop.Model.Name;

			Log.Trace( $"Shooting model: {modelToShoot}" );
		}

		return false;
	}

	void ShootBox()
	{
		var go = new GameObject()
		{
			WorldPosition = Owner.EyeTransform.Position + Owner.EyeTransform.Forward * 50,
			WorldRotation = Owner.EyeTransform.Rotation,
			Tags = { "solid" }
		};

		var prop = go.AddComponent<Prop>();
		prop.Model = Model.Load( modelToShoot );

		go.AddComponent<PropHelper>();

		if ( prop.Components.TryGet<Rigidbody>( out var rigidbody ) )
		{
			rigidbody.Velocity = Owner.EyeTransform.Forward * 1000;
		}
		else if ( prop.Components.TryGet<ModelPhysics>( out var modelPhysics ) )
		{
			modelPhysics.PhysicsGroup.Velocity = Owner.EyeTransform.Forward * 1000;
		}

		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );

		Sandbox.Services.Stats.Increment( "box.shoot", 1 );
	}
}
