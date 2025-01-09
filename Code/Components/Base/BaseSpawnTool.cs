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
				tr.GameObject.GetComponent<PropHelper>().Weld(ent, noCollide: false, fromBone: tr.Bone);
			}
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
			Events.IPropSpawnedEvent.Post( x => x.OnSpawned( prop ) );
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
