using Sandbox.Tools;
[Library( "tool_thruster", Title = "Thruster", Description = "A rocket type thing that can push forwards and backward", Group = "construction" )]
public class Thruster : BaseTool
{
	[Property, Title( "Model" ), ModelProperty( SpawnLists = ["thruster"] )]
	public override string SpawnModel { get; set; } = "models/thruster/thrusterprojector.vmdl";
	[Property, Range( 1, 10000, 10 ), Title("Force Multiplier"), Description("The amount of force the thruster will apply to the attached object.")]
	public float ForceMultiplier { get; set; } = 1000f;
	[Property]
	public bool Massless { get; set; } = true;

	public override void CreatePreview()
	{
		previewModel = new PreviewModel
		{
			ModelPath = GetModel(),
			NormalOffset = 1f,
			RotationOffset = Rotation.From( new Angles( 90, 0, 0 ) ),
			FaceNormal = true
		};
	}
	protected override bool IsPreviewTraceValid( SceneTraceResult tr )
	{
		if ( !IsTraceHit( tr ) )
			return false;

		if ( tr.GameObject.GetComponent<ThrusterComponent>().IsValid() )
			return false;

		return true;
	}

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;

			ThrusterComponent targetThruster = trace.GameObject.GetComponent<ThrusterComponent>();
			if ( targetThruster.IsValid() )
			{
				targetThruster.ForceMultiplier = ForceMultiplier;
				targetThruster.Massless = Massless;

				return true;
			}

			var obj = SpawnIt( trace );

			UndoSystem.Add( creator: this.Owner, callback: ReadyUndo( obj, trace.GameObject ), prop: trace.GameObject );

			PropHelper propHelper = obj.Components.Get<PropHelper>();
			if ( !propHelper.IsValid() || trace.GameObject.IsWorld() )
				return true;

			propHelper.Weld( trace.GameObject );

			return true;
		}

		return false;
	}

	void PositionIt( GameObject wheel, SceneTraceResult trace )
	{
		wheel.WorldPosition = trace.HitPosition + trace.Normal * 1f;
		wheel.WorldRotation = Rotation.LookAt( trace.Normal ) * Rotation.From( new Angles( 90, 0, 0 ) );
	}

	private Func<string> ReadyUndo( GameObject wheel, GameObject other )
	{
		return () =>
		{
			// TODO: When UnHinge works, uncomment this
			wheel.Destroy();
			return "Undid thruster creation";
		};
	}

	GameObject SpawnIt( SceneTraceResult trace )
	{
		var go = new GameObject()
		{
			Tags = { "solid", "thruster" }
		};

		PositionIt( go, trace );

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

		var thruster = go.AddComponent<ThrusterComponent>();
		thruster.ForceMultiplier = ForceMultiplier;
		thruster.Massless = Massless;

		Rigidbody targetBody = trace.GameObject.GetComponent<Rigidbody>();
		if ( targetBody.IsValid() )
		{
			thruster.TargetBody = targetBody;
		}
		else
		{
			var rigid = go.GetOrAddComponent<Rigidbody>();
			thruster.TargetBody = rigid;
		}

		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );
		Sandbox.Events.IPropSpawnedEvent.Post( x => x.OnSpawned( prop ) );

		return go;
	}
}
