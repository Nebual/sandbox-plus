using Sandbox.Physics;

namespace Sandbox.Tools;

[Library( "tool_balloon", Title = "Balloon", Description = "Create Balloons!", Group = "construction" )]
public partial class BalloonTool : BaseSpawnTool
{
	[Property, Range( 0.05f, 2f, 0.05f ), Title( "Balloon Strength" ), Description( "The upward strength of the balloon." )]
	public float BalloonStrength { get; set; } = 0.65f;
	[Property, Range( 5f, 500f, 5f ), Title( "Balloon Rope Length" ), Description( "The length of the rope attached to the balloon." )]
	public float BalloonRopeLength { get; set; } = 100f;
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
		rigid.PhysicsBody.GravityScale = -BalloonStrength;

		UndoSystem.Add( creator: this.Owner, callback: () =>
		{
			go.Destroy();
			return "Undid balloon creation";
		}, prop: go );
		return go;
	}
	protected GameObject SpawnEntity( SceneTraceResult tr, bool useRope )
	{
		var go = SpawnEntity( tr );
		if ( useRope )
		{
			var propHelper = go.GetComponent<PropHelper>();
			propHelper.Rope(tr.GameObject, tr.HitPosition, tr.HitPosition, noCollide: false, toBone: tr.Bone, max: BalloonRopeLength, visualRope: true);
		}
		return go;
	}

	protected override void UpdateEntity( GameObject go )
	{
		go.GetComponent<Rigidbody>().PhysicsBody.GravityScale = -float.Parse( GetConvarValue( "tool_balloon_strength", "0.66" ) );
		go.GetComponent<Prop>().Tint = Tint;
	}
}
