using Sandbox.Physics;

namespace Sandbox.Tools
{
	[Library( "tool_lamp", Title = "Lamp", Description = "Directional light source that casts shadows", Group = "construction" )]
	public partial class LampTool : BaseSpawnTool
	{
		[Property, Title( "Model" ), ModelProperty( SpawnLists = ["lamp"] )]
		public override string SpawnModel { get; set; } = "models/torch/torch.vmdl";

		protected override TypeDescription GetSpawnedComponent()
		{
			return TypeLibrary.GetType<LampComponent>();
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

		protected override void UpdateEntity( GameObject go )
		{
			base.UpdateEntity( go );

			var lamp = go.GetComponent<LampComponent>();
			lamp.Enabled = true;
			lamp.Shadows = true;
			lamp.ConeInner = 25;
			lamp.ConeOuter = 45;
			lamp.Radius = 512;
			lamp.Attenuation = 1;
			lamp.Cookie = Texture.Load( "materials/effects/lightcookies/blank.vtex" );
			go.GetComponent<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.Off; // otherwise the light itself casts shadows from the inside

			go.GetOrAddComponent<LampWireComponent>();
		}

		protected override GameObject SpawnEntity( SceneTraceResult tr )
		{
			var go = base.SpawnEntity( tr );

			var rigid = go.GetComponent<Rigidbody>();
			var bounds = rigid.PhysicsBody.GetBounds();
			go.WorldPosition = tr.EndPosition + tr.Normal * bounds.Size * 0.5f;
			go.WorldRotation = Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 0, 90, 0 ) );
			return go;
		}
		public GameObject SpawnEntity( SceneTraceResult tr, bool useRope )
		{
			var go = SpawnEntity( tr );

			if ( useRope )
			{
				var lengthOffset = float.Parse( GetConvarValue( "tool_balloon_rope_length", "100" ) );
				var body = go.GetComponent<Rigidbody>().PhysicsBody;
				var position1 = body.Transform.PointToWorld( new Vector3( body.GetBounds().Size.x * -1 + 2, 0, 0 ) );

				go.GetComponent<PropHelper>().Rope(tr.GameObject, position1, tr.HitPosition, noCollide: false, toBone: tr.Bone, max: lengthOffset, visualRope: true);
			}
			return go;
		}
	}
}
