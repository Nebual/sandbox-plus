using Sandbox.Tools;

[Library( "tool_wheel", Title = "Wheel", Description = "A wheel that you can turn on and off (but actually can't yet)", Group = "constraints" )]
public class Wheel : BaseTool
{
	[Property, Title( "Wheel Model" ), ModelProperty( SpawnLists = ["wheel"] )]
	public override string SpawnModel { get; set; } = "models/citizen_props/wheel01.vmdl";

	protected override bool IsPreviewTraceValid( SceneTraceResult tr )
	{
		return IsTraceHit( tr ) && !tr.Tags.Contains( "wheel" );
	}

	protected override void OnUpdatePreviewModel( Model model )
	{
		base.OnUpdatePreviewModel( model );
		previewModel.NormalOffset = GetNormalOffset( model );
		previewModel.RotationOffset = GetRotationOffset( model );
	}
	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			if ( trace.Tags.Contains( "wheel" ) || trace.Tags.Contains( "player" ) )
				return true;

			var wheel = SpawnWheel( trace );

			PropHelper propHelper = wheel.Components.Get<PropHelper>();
			if ( !propHelper.IsValid() )
				return true;

			propHelper.Hinge( trace.GameObject, trace.EndPosition, trace.Normal );

			UndoSystem.Add( creator: this.Owner, callback: ReadyUndo( wheel, trace.GameObject ), prop: trace.GameObject );

			return true;
		}

		return false;
	}

	private Func<string> ReadyUndo( GameObject wheel, GameObject other )
	{
		return () =>
		{
			wheel.Destroy();
			return "Undid wheel creation";
		};
	}

	protected float GetNormalOffset( Model model )
	{
		var extents = model.Bounds.Extents;
		if ( extents.x < extents.y && extents.x < extents.z )
		{
			return extents.x - 0.75f;
		}
		else if ( extents.y < extents.x && extents.y < extents.z )
		{
			return extents.y - 0.75f;
		}
		else
		{
			return extents.z - 0.75f;
		}
	}
	protected Rotation GetRotationOffset( Model model )
	{
		var extents = model.Bounds.Extents;
		if ( extents.x < extents.y && extents.x < extents.z )
		{
			return Rotation.From( new Angles( 90, 0, 0 ) );
		}
		else if ( extents.y < extents.x && extents.y < extents.z )
		{
			return Rotation.From( new Angles( 0, -90, 0 ) );
		}
		else
		{
			return Rotation.From( new Angles( 0, 90, 90 ) );
		}
	}

	GameObject SpawnWheel( SceneTraceResult trace )
	{
		var go = new GameObject()
		{
			Tags = { "solid", "wheel" }
		};

		var prop = go.AddComponent<Prop>();
		prop.Model = Model.Load( SpawnModel );

		var propHelper = go.AddComponent<PropHelper>();
		propHelper.Invincible = true;


		go.WorldPosition = trace.HitPosition + trace.Normal * GetNormalOffset( go.GetComponent<Prop>().Model );
		go.WorldRotation = Rotation.LookAt( trace.Normal ) * GetRotationOffset( go.GetComponent<Prop>().Model );

		if ( prop.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
		{
			renderer.CreateBoneObjects = true;
		}

		var rb = propHelper.Rigidbody;
		if ( rb.IsValid() )
		{
			foreach ( var shape in rb.PhysicsBody.Shapes )
			{
				if ( !shape.IsMeshShape )
					continue;

				var newCollider = go.AddComponent<BoxCollider>();
				newCollider.Scale = prop.Model.PhysicsBounds.Size;
			}
		}

		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );
		Sandbox.Events.IPropSpawnedEvent.Post( x => x.OnSpawned( prop ) );

		return go;
	}
}
