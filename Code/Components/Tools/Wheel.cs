[Library( "tool_wheel", Description = "A wheel that you can turn on and off (but actually can't yet)", Group = "construction" )]
public class Wheel : BaseTool
{
	RealTimeSince timeSinceDisabled;

	protected override void OnAwake()
	{
		if ( IsProxy )
			return;
	}
	protected override string GetModel()
	{
		return "models/citizen_props/wheel01.vmdl";
	}
	public override void CreatePreview()
	{
		previewModel = new PreviewModel
		{
			ModelPath = GetModel(),
			NormalOffset = 8f,
			RotationOffset = Rotation.From( new Angles( 0, 90, 0 ) ),
			FaceNormal = true
		};
	}
	protected override bool IsPreviewTraceValid( SceneTraceResult tr )
	{
		return IsTraceHit(tr) && !tr.Tags.Contains("wheel");
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		if ( timeSinceDisabled < Time.Delta * 5f || !Parent.IsValid() )
			return;
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

	void PositionWheel( GameObject wheel, SceneTraceResult trace )
	{
		wheel.WorldPosition = trace.HitPosition + trace.Normal * 8f;
		wheel.WorldRotation = Rotation.LookAt( trace.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) );
	}

	public override void Disabled()
	{
		base.Disabled();
		timeSinceDisabled = 0;
	}

	private Func<string> ReadyUndo(GameObject wheel, GameObject other)
	{
		return () =>
		{
			// TODO: When UnHinge works, uncomment this
			//wheel.GetComponent<PropHelper>().UnHinge( other );
			wheel.Destroy();
			return "Undid wheel creation";
		};
	}

	GameObject SpawnWheel( SceneTraceResult trace )
	{
		var go = new GameObject()
		{
			Tags = { "solid", "wheel" }
		};

		PositionWheel( go, trace );

		var prop = go.AddComponent<Prop>();
		prop.Model = Model.Load( GetModel() );

		var propHelper = go.AddComponent<PropHelper>();
		propHelper.Invincible = true;

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

		return go;
	}
}
