using Sandbox.Physics;

namespace Sandbox.Tools
{
	// a base tool biased to assume "left click: spawn or update settings on an entity"
	public abstract class BaseSpawnTool : BaseTool
	{
		protected virtual bool ShouldWeld { get; set; } = true;
		public override bool Primary( SceneTraceResult tr )
		{
			if ( !Input.Pressed( "attack1" ) )
			{
				return false;
			}
			if ( !tr.Hit || !tr.GameObject.IsValid() )
			{
				return false;
			}

			if ( IsMatchingEntity( tr.GameObject ) )
			{
				UpdateEntity( tr.GameObject );
				return true;
			}

			var ent = SpawnEntity( tr );
			if ( ent.WorldPosition.LengthSquared < 0.1f )
			{
				ent.WorldPosition = tr.EndPosition;
				ent.WorldRotation = Rotation.LookAt( tr.Normal, tr.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) );
			}
			ent.GetComponent<Prop>().Model = Model.Load( GetModel() );

			if ( ShouldWeld && tr.Body.IsValid() && !tr.GameObject.IsWorld() )
			{
				var point1 = PhysicsPoint.World( tr.Body, ent.WorldPosition, ent.WorldRotation );
				var point2 = PhysicsPoint.World( ent.GetComponent<Rigidbody>().PhysicsBody, ent.WorldPosition, ent.WorldRotation );
				var joint = PhysicsJoint.CreateFixed(
					point1,
					point2
				);
				joint.Collisions = false;
				ent.GetComponent<PropHelper>().PhysicsJoints.Add( joint );

				if ( tr.GameObject.GetComponent<PropHelper>() is var propHelper2 && propHelper2.IsValid() )
				{
					propHelper2.PhysicsJoints.Add( joint );
				}
			}

			// Event.Run( "entity.spawned", ent, Owner );
			return true;
		}

		protected virtual GameObject SpawnEntity( SceneTraceResult tr )
		{
			var go = new GameObject()
			{
				WorldPosition = tr.HitPosition,
				WorldRotation = Rotation.LookAt( tr.Normal, tr.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) ),
			};
			var prop = go.AddComponent<Prop>();
			prop.Model = Model.Load( GetModel() );

			go.AddComponent<PropHelper>();
			if ( GetSpawnedComponent() != null )
			{
				go.Components.Create( GetSpawnedComponent() );

				UndoSystem.Add( creator: this.Owner, callback: () =>
				{
					go.Destroy();
					return $"Undid {GetSpawnedComponent().Title} creation";
				}, prop: go );
			}
			UpdateEntity( go );

			go.NetworkSpawn();
			go.Network.SetOrphanedMode( NetworkOrphaned.Host );
			return go;
		}
		protected virtual void UpdateEntity( GameObject ent ) { }

		protected virtual bool IsMatchingEntity( GameObject go )
		{
			return GetSpawnedComponent() != null && go.Components.Get( GetSpawnedComponent().TargetType ) != null;
		}
		protected virtual TypeDescription GetSpawnedComponent()
		{
			return null;
		}
		protected override bool IsPreviewTraceValid( SceneTraceResult tr )
		{
			if ( !IsTraceHit( tr ) )
				return false;

			if ( IsMatchingEntity( tr.GameObject ) )
				return false;

			return true;
		}
	}
}
