
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sandbox.UI;

namespace Sandbox
{
	public partial class VertexMeshBuilder
	{
		public static string CreateCylinderModel( float radius, float depth, int numFaces = 16, int texSize = 64 )
		{
			var key = $"cylinder_{radius}_{depth}_{numFaces}_{texSize}";
			if ( Models.ContainsKey( key ) )
			{
				return key;
			}

			var mins = new Vector3( -radius, -radius, 0f );
			var maxs = new Vector3( radius, radius, depth );
			var bounds = new BBox( mins, maxs );
			var vertexBuilder = new VertexMeshBuilder();
			var vertices = new List<MeshVertex>();
			var collisionVertices = new List<Vector3>();
			
			// add the top and bottom center vertices
			var bottomCenter = new MeshVertex( new Vector3( 0, 0, 0f ), Vector3.Down, Vector3.Zero, new Vector2(0.5f, 0.5f), Color.White );
			var topCenter = new MeshVertex( new Vector3( 0, 0, depth ), Vector3.Up, Vector3.Zero, new Vector2(0.5f, 0.5f), Color.White );
			
			// fill the vertices list with the vertices of the cylinder
			for ( var i = 0; i < numFaces; i++ )
			{
				var angle = 2 * MathF.PI * i / numFaces;
				var angle2 = 2 * MathF.PI * (i + 1) / numFaces;
				var x = radius * MathF.Cos( angle );
				var y = radius * MathF.Sin( angle );
				var x2 = radius * MathF.Cos( angle2 );
				var y2 = radius * MathF.Sin( angle2 );
				
				// calculate the normal direction for this face based on the angle
				var normal =  new Vector3( x, y, 0f ).Normal;
				var normal2 = new Vector3( x2, y2, 0f ).Normal;
				
				// calculate the tangent
				var tangent = Vector3.Cross( normal, Vector3.Up );
				var tangent2 = Vector3.Cross( normal2, Vector3.Up );
				
				// calculate the texture coordinates
				var u = (float)i / numFaces;
				var u2 = (float)(i + 1) / numFaces;
				
				var uv = new Vector2( u, 0f );
				var uv2 = new Vector2( u2, 1f );
				
				// calculate the texture coordinates for the lid
				var uv3 = new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
				var uv4 = new Vector2( MathF.Cos( angle2 ), MathF.Sin( angle2 ) );
				
				// create a quad for each face
				var a = new MeshVertex( new Vector3( x, y, 0f ), normal, tangent, uv, Color.White );
				var b = new MeshVertex( new Vector3( x, y, depth ), normal, tangent, uv2, Color.White );
				var c = new MeshVertex( new Vector3( x2, y2, 0f ), normal2, tangent2, uv, Color.White );
				var d = new MeshVertex( new Vector3( x2, y2, depth ), normal2, tangent2, uv2, Color.White );
				
				var alid = new MeshVertex( new Vector3( x, y, 0f ), Vector3.Down, Vector3.Right, uv3, Color.White );
				var blid = new MeshVertex( new Vector3( x, y, depth ), Vector3.Up, Vector3.Right, uv3, Color.White );
				var clid = new MeshVertex( new Vector3( x2, y2, 0f ), Vector3.Down, Vector3.Right, uv4, Color.White );
				var dlid = new MeshVertex( new Vector3( x2, y2, depth ), Vector3.Up, Vector3.Right, uv4, Color.White );
				
				// add the vertex indices to the list
				vertices.Add( c );
				vertices.Add( b );
				vertices.Add( a );

				vertices.Add( c );
				vertices.Add( d );
				vertices.Add( b );
				
				vertices.Add( bottomCenter );
				vertices.Add( clid );
				vertices.Add( alid );
				
				vertices.Add( blid );
				vertices.Add( dlid );
				vertices.Add( topCenter );
				
				// add the collision vertices to the list
				collisionVertices.Add( c.Position );
				collisionVertices.Add( b.Position );
				collisionVertices.Add( a.Position );
				
				collisionVertices.Add( c.Position );
				collisionVertices.Add( d.Position );
				collisionVertices.Add( b.Position );
				
				collisionVertices.Add( bottomCenter.Position );
				collisionVertices.Add( clid.Position );
				collisionVertices.Add( alid.Position );
				
				collisionVertices.Add( blid.Position );
				collisionVertices.Add( dlid.Position );
				collisionVertices.Add( topCenter.Position );
			}
			
			vertexBuilder.vertices = vertices;
			
			var mesh = new Mesh( Material.Load( "materials/default/vertex_color.vmat" ) );
		
			mesh.CreateVertexBuffer<MeshVertex>( vertexBuilder.vertices.Count, MeshVertex.Layout, vertexBuilder.vertices.ToArray() );
			mesh.SetBounds( mins, maxs );

			var modelBuilder = new ModelBuilder();
			modelBuilder.AddMesh( mesh );
			
			// calculate the mass of the gear
			modelBuilder.WithMass( MathF.PI * radius * radius * depth * 0.0001f );
			
			// generate collision hulls for the cylinder and add them using modelBuilder.AddCollisionHull
			modelBuilder.AddCollisionHull( collisionVertices.ToArray() );

			Models[key] = modelBuilder.Create();
			return key;
		}

		[ClientRpc]
		public static void CreateCylinderModelClient( float radius, float depth, int numFaces, int texSize/* = 64*/ )
		{
			CreateCylinderModel(radius, depth, numFaces, texSize);
		}
		
		[ConCmd.Server( "spawn_dyncylinder" )]
		public static void SpawnCylinder( float radius, float depth, int numFaces = 16, int texScale = 100 )
		{
			if ( ConsoleSystem.Caller == null )
				return;
			
			CreateCylinderModelClient(radius, depth, numFaces, texScale);
			var modelId = CreateCylinderModel(radius, depth, numFaces, texScale);
			
			var entity = SpawnEntity( modelId );
			SandboxPlayer pawn = ConsoleSystem.Caller.Pawn as SandboxPlayer;
			TraceResult trace = Trace.Ray( pawn.EyePosition, pawn.EyePosition + pawn.EyeRotation.Forward * 5000.0f ).UseHitboxes().Ignore( pawn ).Run();

			entity.Position = trace.EndPosition + trace.Normal;
			Event.Run( "entity.spawned", entity, ConsoleSystem.Caller.Pawn );
		}
	}
}
