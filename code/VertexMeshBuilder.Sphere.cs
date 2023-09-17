
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sandbox.UI;

namespace Sandbox
{
	public partial class VertexMeshBuilder
	{
		public static string CreateSphereModel( float radius, int numSegments = 16, int texSize = 64 )
		{
			var key = $"sphere_{radius}_{numSegments}_{texSize}";
			if ( Models.ContainsKey( key ) )
			{
				return key;
			}

			var mins = new Vector3( -radius, -radius, -radius );
			var maxs = new Vector3( radius, radius, radius );
			var bounds = new BBox( mins, maxs );
			var vertexBuilder = new VertexMeshBuilder();
			var vertices = new List<MeshVertex>();
			var indices = new List<int>();
			
			// generate a list of vertices for a sphere
			for (int lat = 0; lat <= numSegments; lat++)
			{
				float theta = lat * MathF.PI / numSegments;
				float sinTheta = MathF.Sin(theta);
				float cosTheta = MathF.Cos(theta);

				for (int lon = 0; lon <= numSegments; lon++)
				{
					float phi = lon * 2.0f * MathF.PI / numSegments;
					float sinPhi = MathF.Sin(phi);
					float cosPhi = MathF.Cos(phi);

					Vector3 position = new Vector3(cosPhi * sinTheta, cosTheta, sinPhi * sinTheta);
					Vector3 normal = position.Normal;
					Vector3 tangent = new Vector3(-sinPhi, 0, cosPhi);
					Vector2 uv = new Vector2((float)lon / numSegments, (float)lat / numSegments);

					vertices.Add(new MeshVertex()
					{
						Position = position * radius,
						Normal = normal,
						Tangent = tangent,
						TexCoord = uv,
						Color = Color.White
					});
				}
			}
			
			for (int lat = 0; lat < numSegments; lat++)
			{
				for (int lon = 0; lon < numSegments; lon++)
				{
					int current = lat * (numSegments + 1) + lon;
					int next = current + numSegments + 1;

					indices.Add(current);
					indices.Add(next + 1);
					indices.Add(next);

					indices.Add(current);
					indices.Add(current + 1);
					indices.Add(next + 1);
				}
			}
			
			vertexBuilder.vertices = vertices;
			
			var mesh = new Mesh( Material.Load( "materials/default/vertex_color.vmat" ) );
		
			mesh.CreateVertexBuffer<MeshVertex>( vertexBuilder.vertices.Count, MeshVertex.Layout, vertexBuilder.vertices.ToArray() );
			mesh.CreateIndexBuffer(indices.Count, indices);
			mesh.SetBounds( mins, maxs );

			var modelBuilder = new ModelBuilder();
			modelBuilder.AddMesh( mesh );
			
			// calculate the mass using the formula for a sphere
			modelBuilder.WithMass( 4.0f / 3.0f * MathF.PI * radius * radius * radius * 0.0001f );
			
			modelBuilder.AddCollisionSphere( radius );

			Models[key] = modelBuilder.Create();
			return key;
		}

		[ClientRpc]
		public static void CreateSphereModelClient( float radius, int numSegments, int texSize/* = 64*/ )
		{
			CreateSphereModel(radius, numSegments, texSize);
		}
		
		[ConCmd.Server( "spawn_dynsphere" )]
		public static void SpawnSphere( float radius, int numSegments = 16, int texScale = 100 )
		{
			if ( ConsoleSystem.Caller == null )
				return;
			
			CreateSphereModelClient(radius, numSegments, texScale);
			var modelId = CreateSphereModel(radius, numSegments, texScale);
			
			var entity = SpawnEntity( modelId );
			SandboxPlayer pawn = ConsoleSystem.Caller.Pawn as SandboxPlayer;
			TraceResult trace = Trace.Ray( pawn.EyePosition, pawn.EyePosition + pawn.EyeRotation.Forward * 5000.0f ).UseHitboxes().Ignore( pawn ).Run();

			entity.Position = trace.EndPosition + trace.Normal;
			Event.Run( "entity.spawned", entity, ConsoleSystem.Caller.Pawn );
		}
	}
}
