using Sandbox.ModelEditor.Nodes;
using Sandbox.Physics;
using Sandbox.Tools;

namespace SandboxPlus;

/// <summary>
/// A component to help deal with props.
/// </summary>
public partial class PropHelper : Component, Component.ICollisionListener
{
	public struct BodyInfo
	{
		public PhysicsBodyType Type { get; set; }
		public Transform Transform { get; set; }
	}

	[Sync] public Guid CreatorId { get; set; } = Guid.Empty;
	[Property, Sync] public float Health { get; set; } = 1f;
	[Property, Sync] public Vector3 Velocity { get; set; } = 0f;
	[Property, Sync] public bool Invincible { get; set; } = false;

	[Sync] public Prop Prop { get; set; }
	[Sync] public ModelPhysics ModelPhysics { get; set; }
	[Sync] public Rigidbody Rigidbody { get; set; }
	private ModelRenderer _renderer;
	public ModelRenderer Renderer
	{
		get
		{
			if ( _renderer == null )
			{
				_renderer = GetComponent<ModelRenderer>();
			}
			return _renderer;
		}
		set => _renderer = value;
	}
	[Sync] public NetDictionary<int, BodyInfo> NetworkedBodies { get; set; } = new();

	private Vector3 lastPosition = Vector3.Zero;

	public int MaterialGroupIndex
	{
		get => Renderer.Model.GetMaterialGroupIndex( Renderer.MaterialGroup );
		set
		{
			if ( value < 0 )
			{
				Renderer.MaterialGroup = "";
			}
			else if ( value < Renderer.Model.MaterialGroupCount )
			{
				Renderer.MaterialGroup = Renderer.Model.GetMaterialGroupName( value );
			}
		}
	}

	protected override void OnStart()
	{
		Prop ??= GetOrAddComponent<Prop>();
		if ( !Prop.IsValid() ) return;
		Prop.OnPropBreak += OnBreak;

		ModelPhysics ??= GetComponent<ModelPhysics>();
		Rigidbody ??= GetComponent<Rigidbody>();
		Renderer ??= GetComponent<ModelRenderer>();

		Health = Prop?.Health ?? 0f;
		Velocity = 0f;

		lastPosition = Prop?.WorldPosition ?? WorldPosition;
		if ( !IsProxy && CreatorId == Guid.Empty )
		{
			CreatorId = Connection.Local.Id;
		}
	}

	[Rpc.Broadcast]
	public void Damage( float amount )
	{
		if ( !Prop.IsValid() ) return;
		if ( IsProxy ) return;
		if ( Health <= 0f ) return;

		Health -= amount;

		if ( Health <= 0f && !Invincible )
			Prop.Kill();
	}

	public void OnBreak()
	{
		var gibs = Prop.CreateGibs();

		if ( gibs.Count > 0 )
		{
			foreach ( var gib in gibs )
			{
				if ( !gib.IsValid() )
					continue;

				gib.Tint = Prop.Tint;
				gib.Tags.Add( "debris" );

				gib.AddComponent<PropHelper>();

				gib.GameObject.NetworkSpawn();
				gib.Network.SetOrphanedMode( NetworkOrphaned.Host );
				Sandbox.Events.IPropSpawnedEvent.Post( x => x.OnSpawned( gib ) );
			}
		}

		if ( Prop.Model.TryGetData<ModelExplosionBehavior>( out var data ) )
		{
			Explosion( data.Effect, data.Sound, WorldPosition, data.Radius, data.Damage, data.Force );
		}

		Prop.Model = null; // Prevents prop from spawning more gibs.
	}

	public void AddForce( int bodyIndex, Vector3 force )
	{
		if ( IsProxy ) return;

		var body = ModelPhysics?.PhysicsGroup?.GetBody( bodyIndex );
		if ( body.IsValid() )
		{
			body.ApplyForce( force );
		}
		else if ( bodyIndex == 0 && Rigidbody.IsValid() )
		{
			Rigidbody.Velocity += force / Rigidbody.PhysicsBody.Mass;
		}
	}

	public async void AddDamagingForce( Vector3 force, float damage )
	{
		if ( IsProxy ) return;

		if ( ModelPhysics.IsValid() )
		{
			foreach ( var body in ModelPhysics.PhysicsGroup.Bodies )
			{
				AddForce( body.GroupIndex, force );
			}
		}
		else
		{
			AddForce( 0, force );
		}

		await Task.DelaySeconds( 1f / Scene.FixedUpdateFrequency + 0.05f );
		if ( !this.IsValid ) return;
		Damage( damage );
	}

	[Rpc.Broadcast]
	public void BroadcastAddForce( int bodyIndex, Vector3 force )
	{
		AddForce( bodyIndex, force );
	}

	[Rpc.Broadcast]
	public void BroadcastAddDamagingForce( Vector3 force, float damage )
	{
		AddDamagingForce( force, damage );
	}

	protected override void OnFixedUpdate()
	{
		if ( Prop.IsValid() )
		{
			Velocity = (Prop.WorldPosition - lastPosition) / Time.Delta;

			lastPosition = Prop.WorldPosition;
		}

		UpdateNetworkedBodies();
	}

	private void UpdateNetworkedBodies()
	{
		if ( !ModelPhysics.IsValid() )
		{
			ModelPhysics = GetComponent<ModelPhysics>();
			Rigidbody = GetComponent<Rigidbody>();

			return;
		}

		if ( !Network.IsOwner )
		{
			var rootBody = FindRootBody();

			foreach ( var (groupId, info) in NetworkedBodies )
			{
				var group = ModelPhysics.PhysicsGroup.GetBody( groupId );
				if ( !group.IsValid() ) continue;

				group.Transform = info.Transform;
				group.BodyType = info.Type;
			}

			if ( rootBody.IsValid() )
				rootBody.Transform = ModelPhysics.Renderer.GameObject.WorldTransform;

			return;
		}

		foreach ( var body in ModelPhysics.PhysicsGroup.Bodies )
		{
			if ( body.GroupIndex == 0 )
				continue;

			var tx = body.GetLerpedTransform( Time.Now );
			NetworkedBodies[body.GroupIndex] = new BodyInfo
			{
				Type = body.BodyType,
				Transform = tx
			};
		}
	}

	private PhysicsBody FindRootBody()
	{
		var body = ModelPhysics.PhysicsGroup.Bodies.FirstOrDefault();
		if ( body == null )
			return null;

		while ( body.Parent.IsValid() )
			body = body.Parent;

		return body;
	}

	private ModelPropData GetModelPropData()
	{
		if ( Prop.Model.IsValid() && !Prop.Model.IsError && Prop.Model.TryGetData( out ModelPropData propData ) )
		{
			return propData;
		}

		ModelPropData defaultData = new()
		{
			Health = -1,
		};

		return defaultData;
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( IsProxy ) return;

		var propData = GetModelPropData();
		if ( propData == null ) return;

		var minImpactSpeed = 500;
		if ( minImpactSpeed <= 0.0f ) minImpactSpeed = 500;

		float impactDmg = Rigidbody.IsValid() ? Rigidbody.Mass / 10 : ModelPhysics.IsValid() ? ModelPhysics.PhysicsGroup.Mass / 10 : 10;
		if ( impactDmg <= 0.0f ) impactDmg = 10;

		float speed = collision.Contact.Speed.Length;

		if ( speed > minImpactSpeed )
		{
			// I take damage from high speed impacts
			if ( Health > 0 )
			{
				var damage = speed / minImpactSpeed * impactDmg;
				Damage( damage );
			}

			var other = collision.Other;

			// Whatever I hit takes more damage
			if ( other.GameObject.IsValid() && other.GameObject != GameObject )
			{
				var damage = speed / minImpactSpeed * impactDmg * 1.2f;

				if ( other.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
				{
					propHelper.Damage( damage );
				}
				else if ( other.GameObject.Root.Components.TryGet<Player>( out var player ) )
				{
					player.TakeDamage( damage );
				}
			}
		}
	}

	public async void Explosion( string particle, string sound, Vector3 position, float radius, float damage, float forceScale )
	{
		await Task.Delay( Game.Random.Next( 50, 250 ) );
		if ( !this.IsValid ) return;

		BroadcastExplosion( sound, position );

		Particles.CreateParticleSystem( particle, new Transform( position, Rotation.Identity ), 10 );

		// Damage, etc
		var overlaps = Game.ActiveScene.FindInPhysics( new Sphere( position, radius ) );

		foreach ( var obj in overlaps )
		{
			if ( !obj.Tags.Intersect( new TagSet() { "solid", "player", "npc", "glass" } ).Any() && obj.Tags.Intersect( new TagSet() { "playercontroller" } ).Any() )
			{
				continue;
			}

			// If the object isn't in line of sight, fuck it off
			var tr = Game.ActiveScene.Trace.Ray( position, obj.WorldPosition )
				.WithoutTags( new TagSet() { "solid", "player", "npc", "glass" }.Append( "solid" ).ToArray() )
				.Run();

			if ( tr.Hit && tr.GameObject.IsValid() )
			{
				if ( !obj.Root.IsDescendant( tr.GameObject ) )
					continue;
			}

			var distance = tr.Hit ? tr.Distance : obj.WorldPosition.Distance( position );
			var distanceMul = 1.0f - Math.Clamp( distance / radius, 0.0f, 1.0f );

			var dmg = damage * distanceMul;

			var force = (obj.WorldPosition - position).Normal * distanceMul * forceScale * 10000f;

			foreach ( var propHelper in obj.Components.GetAll<PropHelper>().ToArray() )
			{
				if ( !propHelper.IsValid() ) continue;

				propHelper.BroadcastAddDamagingForce( force, dmg );
			}

			if ( obj.Root.Components.TryGet<Player>( out var player ) )
			{
				player.TakeDamage( dmg );
			}
		}
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	public static void BroadcastExplosion( string path, Vector3 position )
	{
		if ( string.IsNullOrEmpty( path ) )
		{
			Sound.Play( "rust_pumpshotgun.shootdouble", position );
			return;
		}

		if ( path.StartsWith( "sound/" ) || path.StartsWith( "sounds/" ) )
		{
			var soundEvent = ResourceLibrary.Get<SoundEvent>( path );
			if ( !soundEvent.IsValid() )
			{
				Sound.Play( "rust_pumpshotgun.shootdouble", position );
				return;
			}

			Sound.Play( soundEvent, position );
			return;
		}

		Sound.Play( path, position );
	}
}
