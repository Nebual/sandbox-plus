using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sandbox
{
	public partial class VertexMeshBuilder
	{
		public List<MeshVertex> vertices = new();
		public static Dictionary<string, Model> Models = new();
		public static string CreateRectangleModel( float length, float width, float height, int texSize = 64 )
		{
			var size = new Vector3( length, width, height );
			var key = $"rect_{size.x}_{size.y}_{size.z}_{texSize}";
			if ( Models.ContainsKey( key ) )
			{
				return key;
			}

			var mins = size * -0.5f;
			var maxs = size * 0.5f;
			var vertexBuilder = new VertexMeshBuilder();
			vertexBuilder.AddRectangle( Vector3.Zero, size, texSize, Color.White );

			var mesh = new Mesh( Material.Load( "materials/default/vertex_color.vmat" ) );

			mesh.CreateVertexBuffer<MeshVertex>( vertexBuilder.vertices.Count, MeshVertex.Layout, vertexBuilder.vertices.ToArray() );
			mesh.Bounds = new BBox( mins, maxs );
			GenerateIndices( mesh, vertexBuilder.vertices.Count );

			var modelBuilder = new ModelBuilder();
			modelBuilder.AddMesh( mesh );
			var box = new BBox( mins, maxs );
			modelBuilder.AddCollisionBox( box.Size * 0.5f, box.Center );
			modelBuilder.WithMass( box.Size.x * box.Size.y * box.Size.z * 0.001f );

			Models[key] = modelBuilder.Create();
			return key;
		}

		
		// generate an IndexBuffer. Some meshes (Rectangles, Cylinders, Gears) don't seem to need them in-game, but then won't render in a ScenePanel without em
		private static void GenerateIndices( Mesh mesh, int vertexCount )
		{
			var indices = new List<int>();
			for ( int i = 0; i < vertexCount; i++ )
			{
				indices.Add( i );
			}
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		}

		[ConCmd( "spawn_dynplate" )]
		public static void SpawnPlate( float length, float width, float height, int texSize = 100 )
		{
			var modelId = CreateRectangle( length, width, height, texSize );
			var entity = SpawnEntity( modelId );

			Player player = Player.FindLocalPlayer();
			SceneTraceResult trace = player.BasicTrace();

			entity.WorldPosition = trace.EndPosition + trace.Normal;

			UndoSystem.Add( creator: player, callback: ReadyUndo( entity, "Plate" ), prop: entity );
		}

		private static Func<string> ReadyUndo(GameObject obj, string shape)
		{
			return () =>
			{
				obj.Destroy();
				return "Undone " + shape + " DynShape creation";
			};
		}

		public static GameObject SpawnEntity( string modelId )
		{
			GameObject entity = new() {};
			var renderer = entity.AddComponent<VertexMeshComponent>();
			renderer.ModelId = modelId;
			renderer.Model = Models[modelId];
			
			var prop = entity.AddComponent<Prop>();
			var helper = entity.AddComponent<PropHelper>();
			helper.Invincible = true;
			
			entity.Tags.Add( "solid" );

			entity.NetworkSpawn();
			entity.Network.SetOrphanedMode( NetworkOrphaned.Host );
			Events.IPropSpawnedEvent.Post( x => x.OnSpawned( prop ) );
			
			return entity;
		}

		[Rpc.Broadcast]
		public static void CreateRectangleClient( float length, float width, float height, int texSize )
		{
			CreateRectangleModel( length, width, height, texSize );
		}
		public static string CreateRectangle( float length, float width, float height, int texSize )
		{
			using ( Rpc.FilterExclude( c => c == Connection.Local ) )
				CreateRectangleClient( length, width, height, texSize );
			return CreateRectangleModel( length, width, height, texSize );
		}

		public static void CreateModel( string modelId )
		{
			var split = modelId.Split( '_' );
			if ( modelId.StartsWith( "rect_" ) )
			{
				CreateRectangleModel( split[1].ToFloat(), split[2].ToFloat(), split[3].ToFloat(), split[4].ToInt() );
			}
			else if ( modelId.StartsWith( "cylinder_" ) )
			{
				CreateCylinderModel( split[1].ToFloat(), split[2].ToFloat(), split[3].ToInt(), split[4].ToInt() );
			}
			else if ( modelId.StartsWith( "sphere_" ) )
			{
				CreateSphereModel( split[1].ToFloat(), split[2].ToInt(), split[3].ToInt() );
			}
			else if ( modelId.StartsWith( "gear_" ) )
			{
				CreateGearModel( split[1].ToFloat(), split[2].ToFloat(), split[3].ToInt(), split[4].ToFloat(), split[5].ToFloat(), split[6].ToInt() );
			}
		}

		private void AddRectangle( Vector3 position, Vector3 size, int texSize, Color color = new Color() )
		{
			Rotation rot = Rotation.Identity;

			var f = size.x * rot.Forward * 0.5f;
			var l = size.y * rot.Left * 0.5f;
			var u = size.z * rot.Up * 0.5f;

			CreateQuad( vertices, new Ray( position + f, f.Normal ), l, u, texSize, color );
			CreateQuad( vertices, new Ray( position - f, -f.Normal ), l, -u, texSize, color );

			CreateQuad( vertices, new Ray( position + l, l.Normal ), -f, u, texSize, color );
			CreateQuad( vertices, new Ray( position - l, -l.Normal ), f, u, texSize, color );

			CreateQuad( vertices, new Ray( position + u, u.Normal ), f, l, texSize, color );
			CreateQuad( vertices, new Ray( position - u, -u.Normal ), f, -l, texSize, color );
		}

		public static void CreateQuad( List<MeshVertex> vertices, Ray origin, Vector3 width, Vector3 height, int texSize = 64, Color color = new Color() )
		{
			Vector3 normal = origin.Forward;
			var tangent = width.Normal;

			MeshVertex a = new( origin.Position - width - height, normal, tangent, new Vector2( 0, 0 ), color );
			MeshVertex b = new( origin.Position + width - height, normal, tangent, new Vector2( width.Length / texSize, 0 ), color );
			MeshVertex c = new( origin.Position + width + height, normal, tangent, new Vector2( width.Length / texSize, height.Length / texSize ), color );
			MeshVertex d = new( origin.Position - width + height, normal, tangent, new Vector2( 0, height.Length / texSize ), color );

			vertices.Add( a );
			vertices.Add( b );
			vertices.Add( c );

			vertices.Add( c );
			vertices.Add( d );
			vertices.Add( a );
		}


		[StructLayout( LayoutKind.Sequential )]
		public struct MeshVertex
		{
			public Vector3 Position;
			public Vector3 Normal;
			public Vector3 Tangent;
			public Vector2 TexCoord;
			public Color Color;

			public MeshVertex( Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord, Color color )
			{
				Position = position;
				Normal = normal;
				Tangent = tangent;
				TexCoord = texCoord;
				Color = color;
			}

			public static readonly VertexAttribute[] Layout = {
				new VertexAttribute(VertexAttributeType.Position, VertexAttributeFormat.Float32, 3),
				new VertexAttribute(VertexAttributeType.Normal, VertexAttributeFormat.Float32, 3),
				new VertexAttribute(VertexAttributeType.Tangent, VertexAttributeFormat.Float32, 3),
				new VertexAttribute(VertexAttributeType.TexCoord, VertexAttributeFormat.Float32, 2),
				new VertexAttribute(VertexAttributeType.Color, VertexAttributeFormat.Float32, 4)
			};
		}
	}
}
