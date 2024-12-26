public partial class ThrusterComponent : Component, Component.IPressable
{
	[Property] public float ForceMultiplier { get; set; } = 1.0f;
	[Property] public float Force = 100f;
	[Property] public bool IsForward { get; set; } = true;
	[Property] public bool Massless { get; set; } = true;

	public Rigidbody TargetBody { get; set; }
	private LegacyParticleSystem effects;
	
	private bool _on = false;
	protected bool On
	{
		get
		{
			return _on;
		}
		set
		{
			_on = value;
			if ( _on )
			{
				OnThrusterEnabled();
			}
			else
			{
				OnThrusterDisabled();
			}
		}
	}

	protected void OnThrusterEnabled()
	{
		// Turn emitter on
		if ( effects != null )
			return;

		effects = Particles.MakeParticleSystem( "particles/physgun_end_nohit.vpcf", Transform.World, 0, GameObject );
	}

	protected void OnThrusterDisabled()
	{
		// Turn emitter off
		effects.Destroy();
		effects = null;
	}

	bool IPressable.CanPress( IPressable.Event e )
	{
		return true;
	}
	[Rpc.Broadcast]
	public void Press( GameObject presser )
	{
		if ( presser.Network.Owner != Rpc.Caller )
			return;

		On = !On;
		Sound.Play( On ? "drop_001" : "drop_002", WorldPosition );
	}

	bool IPressable.Press( IPressable.Event e )
	{
		Press( e.Source.GameObject );
		return true;
	}

	protected override void OnFixedUpdate()
	{
		if ( On )
		{
			TargetBody.ApplyForceAt( 
				WorldPosition, 
				GameObject.WorldRotation.Down * (
					Massless ? 
						Force * ForceMultiplier * TargetBody.Mass * (IsForward ? 1 : -1) 
						: 
						Force * (IsForward ? 1 : -1)
				) 
			);
			var bounds = GetComponent<Prop>().Model.Bounds;
			effects.WorldPosition = Transform.World.PointToWorld(bounds.Center + new Vector3(0,0,bounds.Extents.z));
		}
	}
}
