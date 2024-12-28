using Sandbox.UI;

[Library( "tool_thruster", Description = "A rocket type thing that can push forwards and backward", Group = "construction" )]
public class Thruster : BaseTool
{
	RealTimeSince timeSinceDisabled;

	[ConVar( "tool_thruster_thrust" )]
	public static float _ { get; set; } = 1000f;

	private static Slider WeightSlider;

	protected override void OnAwake()
	{
		if ( IsProxy )
			return;
	}
	protected override string GetModel()
	{
		return "models/thruster/thrusterprojector.vmdl";
	}
	private string tagName()
	{
		return "thruster";
	}
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
				targetThruster.ForceMultiplier = float.Parse( GetConvarValue( "tool_thruster_thrust" ) );

				return true;
			}

			var obj = SpawnIt( trace );

			UndoSystem.Add( creator: this.Owner, callback: ReadyUndo( obj, trace.GameObject ), prop: trace.GameObject );

			PropHelper propHelper = obj.Components.Get<PropHelper>();
			if ( !propHelper.IsValid() )
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

	public override void Disabled()
	{
		base.Disabled();
		timeSinceDisabled = 0;
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
		thruster.ForceMultiplier = float.Parse( GetConvarValue( "tool_thruster_thrust" ) );

		Rigidbody targetBody = trace.GameObject.GetComponent<Rigidbody>();
		if ( targetBody.IsValid() )
		{
			thruster.TargetBody = targetBody;
		}
		else
		{
			var rigid = go.AddComponent<Rigidbody>();
			thruster.TargetBody = rigid;
		}

			go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );

		return go;
	}

	[Rpc.Owner]
	public void SetThrustConvar( float thrust )
	{
		ConsoleSystem.Run( "tool_thruster_thrust", thrust );
		if ( WeightSlider.IsValid() )
		{
			WeightSlider.Value = thrust;
		}
		OnThrustConvarChanged( thrust );
		HintFeed.AddHint( "", $"Loaded thrust of {thrust}" );
	}
	public void OnThrustConvarChanged( float thrust )
	{
		Description = $"Set thrust to {thrust}";
	}

	public override void CreateToolPanel()
	{
		WeightSlider = new Slider
		{
			Label = "Thrust",
			Min = 0f,
			Max = 10000f,
			Value = 1f,
			Step = 1f,
			Convar = "tool_thruster_thrust",
			OnValueChanged = OnThrustConvarChanged,
		};
		SpawnMenu.Instance?.ToolPanel?.AddChild( WeightSlider );
	}
}
