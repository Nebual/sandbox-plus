using Sandbox.Physics;

namespace Sandbox.Tools
{
	[Library( "tool_lamp", Title = "Lamps", Description = "Directional light source that casts shadows", Group = "construction" )]
	public partial class LampTool : BaseTool
	{
		protected override string GetModel()
		{
			return "models/torch/torch.vmdl";
		}

		protected override bool IsPreviewTraceValid( SceneTraceResult tr )
		{
			if ( !IsTraceHit( tr ) )
				return false;

			if ( tr.GameObject.GetComponent<LampComponent>().IsValid() )
				return false;

			return true;
		}

		public override void CreatePreview()
		{
			var m = Model.Load( GetModel() );
			previewModel = new PreviewModel
			{
				ModelPath = GetModel(),
				NormalOffset = m.Bounds.Size.z * 0.5f,
				RotationOffset = Rotation.From( new Angles( 0, 90, 0 ) ),
				FaceNormal = true
			};
		}

		public override bool Primary( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
			if ( !Input.Pressed( "attack1" ) ) return false;

			if ( trace.GameObject.GetComponent<LampComponent>().IsValid() )
			{
				// todo update (once any properties are configurable)
				return true;
			}
			Spawn( trace, false );
			return true;
		}
		public override bool Secondary( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
			if ( !Input.Pressed( "attack2" ) ) return false;

			Spawn( trace, true );
			return true;
		}

		public void Spawn( SceneTraceResult tr, bool useRope )
		{
			var go = new GameObject()
			{
				WorldPosition = tr.HitPosition + tr.Normal * 8f,
				WorldRotation = Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) ),
				Tags = { "lamp" },
			};
			var prop = go.AddComponent<Prop>();
			prop.Model = Model.Load( GetModel() );
			var propHelper = go.AddComponent<PropHelper>();
			var rigid = go.GetComponent<Rigidbody>();
			var body = rigid.PhysicsBody;
			var bounds = rigid.PhysicsBody.GetBounds();

			var lamp = go.AddComponent<LampComponent>();
			lamp.Enabled = true;
			lamp.Shadows = true;
			lamp.ConeInner = 25;
			lamp.ConeOuter = 45;
			lamp.Radius = 512;
			lamp.Attenuation = 0.2f;
			lamp.Cookie = Texture.Load( "materials/effects/lightcookie.vtex" );

			go.WorldPosition = tr.EndPosition + tr.Normal * bounds.Size * 0.5f;

			go.NetworkSpawn();
			go.Network.SetOrphanedMode( NetworkOrphaned.Host );

			if ( useRope )
			{
				var point1 = PhysicsPoint.World( tr.Body, tr.EndPosition, tr.Body.Rotation );
				var position2 = body.Transform.PointToWorld( new Vector3( bounds.Size.x * -1 + 2, 0, 0 ) );
				var point2 = PhysicsPoint.World( body, position2, body.Rotation );
				var lengthOffset = float.Parse( GetConvarValue( "tool_balloon_rope_length", "100" ) );
				var length = lengthOffset;
				var joint = PhysicsJoint.CreateLength(
					point1,
					point2,
					length
				);
				joint.SpringLinear = new( 1000.0f, 0.7f );
				joint.Collisions = true;

				var rope = ConstraintTool.MakeRope( body, position2, tr.Body, tr.HitPosition );

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
				return "Undid lamp creation";
			}, prop: go );
			// Event.Run( "entity.spawned", ent, Owner );
		}
	}
}
