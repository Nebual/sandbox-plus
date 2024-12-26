using Sandbox.Physics;

namespace Sandbox.Tools
{
	[Library( "tool_lamp", Title = "Lamps", Description = "Directional light source that casts shadows", Group = "construction" )]
	public partial class LampTool : BaseSpawnTool
	{
		protected override string GetModel()
		{
			return "models/torch/torch.vmdl";
		}

		protected override bool IsMatchingEntity( GameObject go )
		{
			return go.GetComponent<LampComponent>().IsValid();
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

		public override bool Secondary( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() || trace.Tags.Contains( "player" ) ) return false;
			if ( !Input.Pressed( "attack2" ) ) return false;

			SpawnEntity( trace, true );
			return true;
		}

		protected override GameObject SpawnEntity( SceneTraceResult tr )
		{
			var go = base.SpawnEntity( tr );
			go.WorldRotation = Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) );
			go.Tags.Add( "lamp" );

			var rigid = go.GetComponent<Rigidbody>();
			var bounds = rigid.PhysicsBody.GetBounds();
			var lamp = go.AddComponent<LampComponent>();
			lamp.Enabled = true;
			lamp.Shadows = true;
			lamp.ConeInner = 25;
			lamp.ConeOuter = 45;
			lamp.Radius = 512;
			lamp.Attenuation = 0.2f;
			lamp.Cookie = Texture.Load( "materials/effects/lightcookie.vtex" );
			go.AddComponent<LampWireComponent>();

			go.WorldPosition = tr.EndPosition + tr.Normal * bounds.Size * 0.5f;
			UndoSystem.Add( creator: this.Owner, callback: () =>
			{
				go.Destroy();
				return "Undid lamp creation";
			}, prop: go );
			// Event.Run( "entity.spawned", ent, Owner );
			return go;
		}
		public GameObject SpawnEntity( SceneTraceResult tr, bool useRope )
		{
			var go = SpawnEntity( tr );

			if ( useRope )
			{
				var body = go.GetComponent<Rigidbody>().PhysicsBody;
				var bounds = body.GetBounds();

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
	}
}
