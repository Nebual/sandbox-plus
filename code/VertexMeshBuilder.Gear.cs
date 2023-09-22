using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sandbox.UI;

namespace Sandbox
{
	public partial class VertexMeshBuilder
	{
		public static string CreateGearModel( float radius, float depth, int numTeeth /* = 16*/, float cutDepth /* = 0.1f*/, float cutAngle /* = 5f*/, int texSize /* = 64*/ )
		{
			var key = $"gear_{radius}_{depth}_{numTeeth}_{cutDepth}_{cutAngle}_{texSize}";
			if ( Models.ContainsKey( key ) )
			{
				return key;
			}

			var mins = new Vector3( -radius, -radius, -depth * 0.5f );
			var maxs = new Vector3( radius, radius, depth * 0.5f );
			var vertexBuilder = new VertexMeshBuilder();
			
			var vertices = new List<MeshVertex>();

			float anglePerTooth = 2 * MathF.PI / numTeeth;
			float cutAngleInRadians = (MathF.PI / 180) * cutAngle;
			float innerAnglePerTooth = anglePerTooth - (cutAngleInRadians * 2);
			float innerRadius = radius - cutDepth;

			var innerCollisionVertices = new List<Vector3>();
			var toothCollisionHulls = new List<List<Vector3>>();

			// add the top and bottom center vertices
			var bottomCenter = new MeshVertex( new Vector3( 0, 0, 0f ), Vector3.Down, Vector3.Zero, new Vector2( 0.5f, 0.5f ), Color.White );
			var topCenter = new MeshVertex( new Vector3( 0, 0, depth ), Vector3.Up, Vector3.Zero, new Vector2( 0.5f, 0.5f ), Color.White );

			for ( int i = 0; i < numTeeth; ++i )
			{
				float startAngle = i * anglePerTooth;
				float innerStartAngle = startAngle + cutAngleInRadians;
				float innerEndAngle = innerStartAngle + (innerAnglePerTooth * 0.5f);
				float outerAngle = innerEndAngle + cutAngleInRadians;
				float endAngle = startAngle + anglePerTooth;

				var depthOffset = new Vector3( 0, 0, depth );
				var v1Lower = new Vector3( radius * MathF.Cos( startAngle ), radius * MathF.Sin( startAngle ), 0 );
				var v2Lower = new Vector3( innerRadius * MathF.Cos( innerStartAngle ), innerRadius * MathF.Sin( innerStartAngle ), 0 );
				var v3Lower = new Vector3( innerRadius * MathF.Cos( innerEndAngle ), innerRadius * MathF.Sin( innerEndAngle ), 0 );
				var v4Lower = new Vector3( radius * MathF.Cos( outerAngle ), radius * MathF.Sin( outerAngle ), 0 );
				var v5Lower = new Vector3( radius * MathF.Cos( endAngle ), radius * MathF.Sin( endAngle ), 0 );
				var v1Upper = v1Lower + depthOffset;
				var v2Upper = v2Lower + depthOffset;
				var v3Upper = v3Lower + depthOffset;
				var v4Upper = v4Lower + depthOffset;
				var v5Upper = v5Lower + depthOffset;

				var vertex1Lower = new MeshVertex( v1Lower, v1Lower.Normal, v1Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex2Lower = new MeshVertex( v2Lower, v2Lower.Normal, v2Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex3Lower = new MeshVertex( v3Lower, v3Lower.Normal, v3Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex4Lower = new MeshVertex( v4Lower, v4Lower.Normal, v4Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex5Lower = new MeshVertex( v5Lower, v5Lower.Normal, v5Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex1Upper = new MeshVertex( v1Upper, v1Lower.Normal, v1Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex2Upper = new MeshVertex( v2Upper, v2Lower.Normal, v2Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex3Upper = new MeshVertex( v3Upper, v3Lower.Normal, v3Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex4Upper = new MeshVertex( v4Upper, v4Lower.Normal, v4Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );
				var vertex5Upper = new MeshVertex( v5Upper, v5Lower.Normal, v5Lower.Normal.Cross(Vector3.Up), new Vector2( 0, 0 ), Color.White );

				// add the sides
				vertices.Add( vertex2Lower );
				vertices.Add( vertex1Upper );
				vertices.Add( vertex1Lower );

				vertices.Add( vertex1Upper );
				vertices.Add( vertex2Lower );
				vertices.Add( vertex2Upper );

				vertices.Add( vertex2Upper );
				vertices.Add( vertex2Lower );
				vertices.Add( vertex3Upper );

				vertices.Add( vertex2Lower );
				vertices.Add( vertex3Lower );
				vertices.Add( vertex3Upper );

				vertices.Add( vertex3Upper );
				vertices.Add( vertex3Lower );
				vertices.Add( vertex4Lower );

				vertices.Add( vertex3Upper );
				vertices.Add( vertex4Lower );
				vertices.Add( vertex4Upper );

				vertices.Add( vertex4Upper );
				vertices.Add( vertex4Lower );
				vertices.Add( vertex5Lower );

				vertices.Add( vertex4Upper );
				vertices.Add( vertex5Lower );
				vertices.Add( vertex5Upper );

				// add the lids
				var vertex1LidLower = new MeshVertex( v1Lower, Vector3.Down, Vector3.Right, new Vector2( v1Lower ).Normal, Color.White );
				var vertex2LidLower = new MeshVertex( v2Lower, Vector3.Down, Vector3.Right, new Vector2( v2Lower ).Normal, Color.White );
				var vertex3LidLower = new MeshVertex( v3Lower, Vector3.Down, Vector3.Right, new Vector2( v3Lower ).Normal, Color.White );
				var vertex4LidLower = new MeshVertex( v4Lower, Vector3.Down, Vector3.Right, new Vector2( v4Lower ).Normal, Color.White );
				var vertex5LidLower = new MeshVertex( v5Lower, Vector3.Down, Vector3.Right, new Vector2( v5Lower ).Normal, Color.White );
				var vertex1LidUpper = new MeshVertex( v1Upper, Vector3.Up, Vector3.Right, new Vector2( v1Upper ).Normal, Color.White );
				var vertex2LidUpper = new MeshVertex( v2Upper, Vector3.Up, Vector3.Right, new Vector2( v2Upper ).Normal, Color.White );
				var vertex3LidUpper = new MeshVertex( v3Upper, Vector3.Up, Vector3.Right, new Vector2( v3Upper ).Normal, Color.White );
				var vertex4LidUpper = new MeshVertex( v4Upper, Vector3.Up, Vector3.Right, new Vector2( v4Upper ).Normal, Color.White );
				var vertex5LidUpper = new MeshVertex( v5Upper, Vector3.Up, Vector3.Right, new Vector2( v5Upper ).Normal, Color.White );

				vertices.Add( bottomCenter );
				vertices.Add( vertex2LidLower );
				vertices.Add( vertex1LidLower );

				vertices.Add( bottomCenter );
				vertices.Add( vertex3LidLower );
				vertices.Add( vertex2LidLower );

				vertices.Add( bottomCenter );
				vertices.Add( vertex4LidLower );
				vertices.Add( vertex3LidLower );

				vertices.Add( bottomCenter );
				vertices.Add( vertex5LidLower );
				vertices.Add( vertex4LidLower );

				vertices.Add( vertex1LidUpper );
				vertices.Add( vertex2LidUpper );
				vertices.Add( topCenter );

				vertices.Add( vertex2LidUpper );
				vertices.Add( vertex3LidUpper );
				vertices.Add( topCenter );

				vertices.Add( vertex3LidUpper );
				vertices.Add( vertex4LidUpper );
				vertices.Add( topCenter );

				vertices.Add( vertex4LidUpper );
				vertices.Add( vertex5LidUpper );
				vertices.Add( topCenter );
				
				// add the inner collisions
				innerCollisionVertices.Add( vertex2Upper.Position );
				innerCollisionVertices.Add( vertex2Lower.Position );
				innerCollisionVertices.Add( vertex3Upper.Position );
				innerCollisionVertices.Add( vertex2Lower.Position );
				innerCollisionVertices.Add( vertex3Lower.Position );
				innerCollisionVertices.Add( vertex3Upper.Position );
				innerCollisionVertices.Add( vertex2LidUpper.Position );
				innerCollisionVertices.Add( vertex3LidUpper.Position );
				innerCollisionVertices.Add( topCenter.Position );
				
				// add the tooth collisions
				float collisionStartAngle = innerEndAngle;
				float collisionOuterAngle1 = outerAngle;
				float collisionOuterAngle2 = endAngle;
				float collisionEndAngle = endAngle + cutAngleInRadians;

				var collision1Lower = new Vector3( innerRadius * MathF.Cos( collisionStartAngle ), innerRadius * MathF.Sin( collisionStartAngle ), 0 );
				var collision2Lower = new Vector3( radius * MathF.Cos( collisionOuterAngle1 ), radius * MathF.Sin( collisionOuterAngle1 ), 0 );
				var collision3Lower = new Vector3( radius * MathF.Cos( collisionOuterAngle2 ), radius * MathF.Sin( collisionOuterAngle2 ), 0 );
				var collision4Lower = new Vector3( innerRadius * MathF.Cos( collisionEndAngle ), innerRadius * MathF.Sin( collisionEndAngle ), 0 );
				var collision1Upper = collision1Lower + depthOffset;
				var collision2Upper = collision2Lower + depthOffset;
				var collision3Upper = collision3Lower + depthOffset;
				var collision4Upper = collision4Lower + depthOffset;
				
				var toothCollisionVertices = new List<Vector3>
				{
					collision2Lower,
					collision1Upper,
					collision1Lower,
					
					collision1Upper,
					collision2Lower,
					collision2Upper,
					
					collision2Upper,
					collision2Lower,
					collision3Upper,
					
					collision2Lower,
					collision3Lower,
					collision3Upper,
					
					collision3Upper,
					collision3Lower,
					collision4Lower,
					
					collision3Upper,
					collision4Lower,
					collision4Upper
				};

				toothCollisionHulls.Add( toothCollisionVertices );
			}

			vertexBuilder.vertices = vertices;

			var mesh = new Mesh( Material.Load( "materials/default/vertex_color.vmat" ) );

			mesh.CreateVertexBuffer<MeshVertex>( vertexBuilder.vertices.Count, MeshVertex.Layout, vertexBuilder.vertices.ToArray() );
			mesh.SetBounds( mins, maxs );
			GenerateIndices( mesh, vertexBuilder.vertices.Count );

			var modelBuilder = new ModelBuilder();
			modelBuilder.AddMesh( mesh );

			// calculate the mass of the gear
			modelBuilder.WithMass( MathF.PI * radius * radius * depth * 0.001f );

			// add the collision hulls
			modelBuilder.AddCollisionHull( innerCollisionVertices.ToArray() );
			toothCollisionHulls.ForEach( hull => modelBuilder.AddCollisionHull( hull.ToArray() ) );

			Models[key] = modelBuilder.Create();
			return key;
		}

		[ClientRpc]
		public static void CreateGearModelClient( float radius, float depth, int numTeeth /* = 16*/, float cutDepth /* = 0.1f*/, float cutAngle /* = 5f*/, int texSize /* = 64*/ )
		{
			CreateGearModel( radius, depth, numTeeth, cutDepth, cutAngle, texSize );
		}
		public static string CreateGear( float radius, float depth, int numTeeth, float cutDepth, float cutAngle, int texSize )
		{
			CreateGearModelClient( radius, depth, numTeeth, cutDepth, cutAngle, texSize );
			return CreateGearModel( radius, depth, numTeeth, cutDepth, cutAngle, texSize );
		}

		[ConCmd.Server( "spawn_dyngear" )]
		public static void SpawnGear( float radius, float depth, int numTeeth /* = 16*/, float cutDepth /* = 0.1f*/, float cutAngle /* = 5f*/, int texScale = 100 )
		{
			if ( ConsoleSystem.Caller == null )
				return;

			var modelId = CreateGear( radius, depth, numTeeth, cutDepth, cutAngle, texScale );

			var entity = SpawnEntity( modelId );
			SandboxPlayer pawn = ConsoleSystem.Caller.Pawn as SandboxPlayer;
			TraceResult trace = Trace.Ray( pawn.EyePosition, pawn.EyePosition + pawn.EyeRotation.Forward * 5000.0f ).UseHitboxes().Ignore( pawn ).Run();

			entity.Position = trace.EndPosition + trace.Normal;
			Event.Run( "entity.spawned", entity, ConsoleSystem.Caller.Pawn );
		}
	}
}
