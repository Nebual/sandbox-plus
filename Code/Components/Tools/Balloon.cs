using Sandbox.Physics;

namespace Sandbox.Tools;

[Library( "tool_balloon", Title = "Balloons", Description = "Create Balloons!", Group = "construction" )]
public partial class BalloonTool : BaseSpawnTool
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

	protected override bool IsMatchingEntity( GameObject go )
	{
		return go.Tags.Has( "balloon" );
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

		if ( IsMatchingEntity( trace.GameObject ) )
		{
			UpdateEntity( trace.GameObject );
			Tint = Color.Random;
			return true;
		}
		SpawnEntity( trace, true );
		return true;

	}
	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
		if ( !Input.Pressed( "attack2" ) ) return false;

		SpawnEntity( trace, false );
		return true;
	}
	protected override GameObject SpawnEntity( SceneTraceResult tr )
	{
		var go = base.SpawnEntity( tr );
		go.WorldPosition = tr.HitPosition + tr.Normal * 8f;
		go.WorldRotation = Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) );
		go.Tags.Add( "balloon" );
		var prop = go.GetComponent<Prop>();
		prop.Tint = Tint;
		prop.OnPropBreak += () =>
		{
			Sound.Play( "balloon_pop_cute", prop.WorldPosition );
		};
		Tint = Color.Random;

		var rigid = go.GetComponent<Rigidbody>();
		rigid.PhysicsBody.GravityScale = -float.Parse( GetConvarValue( "tool_balloon_strength", "0.66" ) );

		UndoSystem.Add( creator: this.Owner, callback: () =>
		{
			go.Destroy();
			return "Undid balloon creation";
		}, prop: go );
		// Event.Run( "entity.spawned", ent, Owner );
		return go;
	}
	protected GameObject SpawnEntity( SceneTraceResult tr, bool useRope )
	{
		var go = SpawnEntity( tr );
		if ( useRope )
		{
			var body = go.GetComponent<Rigidbody>().PhysicsBody;
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
			go.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			joint.OnBreak += () =>
			{
				rope?.Destroy();
				joint.Remove();
			};
			go.GetComponent<Prop>().OnPropBreak += () =>
			{
				rope?.Destroy();
				joint.Remove();
			};
		}
		return go;
	}

	protected override void UpdateEntity( GameObject go )
	{
		go.GetComponent<Rigidbody>().PhysicsBody.GravityScale = -float.Parse( GetConvarValue( "tool_balloon_strength", "0.66" ) );
		go.GetComponent<Prop>().Tint = Tint;
	}

	public override void CreateToolPanel()
	{
		SpawnMenu.Instance?.ToolPanel?.AddChild( new BalloonToolConfig() );
	}
}
