using Sandbox.Physics;

namespace Sandbox.Tools;

[Library( "tool_balloon", Title = "Balloons", Description = "Create Balloons!", Group = "construction" )]
public partial class BalloonTool : BaseTool
{
	[ConVar( "tool_balloon_strength" )] public static string _ { get; set; } = "0.65";
	[ConVar( "tool_balloon_rope_length" )] public static string _2 { get; set; } = "100";
	public Color Tint { get; set; }

	public override void Activate()
	{
		base.Activate();

		Tint = Color.Random;
	}

	protected override string GetModel()
	{
		return "models/citizen_props/balloonregular01.vmdl";
	}

	protected override bool IsPreviewTraceValid( SceneTraceResult tr )
	{
		if ( !IsTraceHit( tr ) )
			return false;

		if ( tr.GameObject.Tags.Has( "balloon" ) )
			return false;

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		if ( previewModel != null )
		{
			previewModel.SetTint( Tint );
		}
	}
	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
		if ( !Input.Pressed( "attack1" ) ) return false;

		if ( trace.Tags.Contains( "balloon" ) )
		{
			trace.GameObject.GetComponent<Rigidbody>().PhysicsBody.GravityScale = -float.Parse( GetConvarValue( "tool_balloon_strength", "0.66" ) );
			trace.GameObject.GetComponent<Prop>().Tint = Tint;
			Tint = Color.Random;
			return true;
		}
		Spawn( trace, true );
		return true;

	}
	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
		if ( !Input.Pressed( "attack2" ) ) return false;

		Spawn( trace, false );
		return true;
	}
	public void Spawn( SceneTraceResult tr, bool useRope )
	{
		var go = new GameObject()
		{
			WorldPosition = tr.HitPosition + tr.Normal * 8f,
			WorldRotation = Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) ),
			Tags = { "balloon" },
		};
		var prop = go.AddComponent<Prop>();
		prop.Model = Model.Load( GetModel() );
		prop.Tint = Tint;
		prop.OnPropBreak += () =>
		{
			Sound.Play( "balloon_pop_cute", prop.WorldPosition );
		};
		Tint = Color.Random;

		var propHelper = go.AddComponent<PropHelper>();
		var rigid = go.GetComponent<Rigidbody>();
		var body = rigid.PhysicsBody;
		body.GravityScale = -float.Parse( GetConvarValue( "tool_balloon_strength", "0.66" ) );

		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );

		if ( useRope )
		{

			var point1 = PhysicsPoint.World( tr.Body, tr.EndPosition, tr.Body.Rotation );
			var point2 = PhysicsPoint.World( body, tr.EndPosition, body.Rotation );
			var lengthOffset = float.Parse( GetConvarValue( "tool_balloon_rope_length", "100" ) );
			var length = lengthOffset;
			var joint = PhysicsJoint.CreateLength(
				point1,
				point2,
				length
			);
			joint.SpringLinear = new( 1000.0f, 0.7f );
			joint.Collisions = true;

			var rope = ConstraintTool.MakeRope( body, go.WorldPosition, tr.Body, tr.HitPosition );

			var trPropHelper = tr.GameObject.GetComponent<PropHelper>();
			if ( trPropHelper.IsValid() )
			{
				trPropHelper.PhysicsJoints.Add( joint );
			}
			propHelper.PhysicsJoints.Add( joint );
			joint.OnBreak += () =>
			{
				rope?.Destroy();
				joint.Remove();
			};
			prop.OnPropBreak += () =>
			{
				rope?.Destroy();
				joint.Remove();
			};
		}
		UndoSystem.Add( creator: this.Owner, callback: () =>
		{
			go.Destroy();
			return "Undid balloon creation";
		}, prop: go );
		// Event.Run( "entity.spawned", ent, Owner );
	}

	public override void CreateToolPanel()
	{
		SpawnMenu.Instance?.ToolPanel?.AddChild( new BalloonToolConfig() );
	}
}
