using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sandbox
{
	public interface Duplicatable
	{
		// Called while copying to store entity data
		public virtual Dictionary<string, object> PreDuplicatorCopy() { return new Dictionary<string, object>(); }

		// Called after the duplicator has finished creating this entity
		public virtual void PostDuplicatorPaste( Dictionary<string, object> data ) { }

		// Called after all entities are created
		public virtual void PostDuplicatorPasteEntities( Dictionary<int, Entity> entities ) { }

		// Called after pasting is done
		public virtual void PostDuplicatorPasteDone( Dictionary<int, Entity> entities ) { }

	}

	public class DuplicatorEncoder
	{
		public static DuplicatorData Decode( byte[] data )
		{
			using ( var stream = new MemoryStream( data ) )
			using ( var bn = new BinaryReader( stream ) )
			{
				if ( bn.ReadUInt32() != 0x44555045 ) throw new Exception( "The file isn't a duplicator file!" );
				int ver = (int)bn.ReadByte();
				switch ( ver )
				{
					case 0:
						return DecodeV0( bn );
					default:
						throw new Exception( "Invalid encoder version: " + ver );
				}
			}
		}

		static void writeString( BinaryWriter bn, string s )
		{
			bn.Write( s.Length );
			bn.Write( Encoding.ASCII.GetBytes( s ) );
		}
		static string readString( BinaryReader bn )
		{
			return Encoding.ASCII.GetString( bn.ReadBytes( bn.ReadInt32() ) );
		}

		static byte[] Encode( DuplicatorData entityData )
		{
			using ( var stream = new MemoryStream() )
			using ( var bn = new BinaryWriter( stream ) )
			{
				bn.Write( (uint)0x44555045 ); // File type 'DUPE'
				bn.Write( (byte)0 ); // Encoder version
				writeString( bn, entityData.name );
				writeString( bn, entityData.author );
				writeString( bn, entityData.date );

				bn.Write( (uint)entityData.entities.Count );
				foreach(DuplicatorData.DuplicatorItem item in entityData.entities )
				{

				}
				bn.Write( (uint)entityData.constraints.Count );
				foreach ( DuplicatorData.DuplicatorItem item in entityData.entities )
				{

				}

				return stream.GetBuffer();
			}
		}

		static DuplicatorData DecodeV0( BinaryReader reader )
		{
			return new DuplicatorData();
		}

		static string EncodeJson( DuplicatorData entityData )
		{
			return JsonSerializer.Serialize( entityData, new JsonSerializerOptions { WriteIndented = true } );
		}

		static DuplicatorData DecodeJsonV0( string data )
		{
			return (DuplicatorData)JsonSerializer.Deserialize( data, typeof( DuplicatorData ) );
		}
	}

	public class DuplicatorData
	{
		public class DuplicatorItem
		{
			public string className;
			public string model;
			public Vector3 position;
			public Rotation angles;
			public List<object> userData = new List<object>();
		}
		public class DuplicatorConstraint
		{
			public int entIndex1;
			public int entIndex2;
			public int bone1;
			public int bone2;
			public string type;
		}

		public string name = "";
		public string author = "";
		public string date = "";
		public List<DuplicatorItem> entities = new List<DuplicatorItem>();
		public List<DuplicatorConstraint> constraints = new List<DuplicatorConstraint>();
		public void Clear() { name = ""; author = ""; date = ""; entities.Clear(); constraints.Clear(); }
		public void Add(Entity ent)
		{

		}
	}
}

namespace Sandbox.Tools
{
	[Library( "tool_duplicator", Title = "Duplicator", Description = "Save and load contraptions", Group = "construction" )]
	public partial class DuplicatorTool : BaseTool
	{
		static HashSet<string> GenericClasses = new HashSet<string>();
		static void GenericFactory( string classname )
		{
			var ent = Library.Create<Prop>( classname );
			//ent.Position = ;
			//ent.Rotation = ;
			//ent.PhysicsEnabled = ;
			//ent.EnableSolidCollisions =;
			//ent.Massless = ;
		}

		// Default behavior will be restoring the freeze state of entities to what they were when copied
		[ConVar.ClientData( "tool_duplicator_freeze_all", Help = "Freeze all entities during pasting", Saved = true )]
		public bool FreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_unfreeze_all", Help = "Unfreeze all entities after pasting", Saved = true )]
		public bool UnfreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_area_size", Help = "Area copy size", Saved = true )]
		public float AreaSize { get; set; } = 250;


		List<PreviewEntity> ghosts = new List<PreviewEntity>();
		public override void CreatePreviews()
		{

		}

		void GetAttachedEntities( Entity baseEnt )
		{
			HashSet<Entity> entsChecked = new();
			Stack<Entity> entsToCheck = new();
			entsToCheck.Push( baseEnt );
			while ( entsToCheck.Count > 0 )
			{
				Entity ent = entsToCheck.Pop();
				if ( entsChecked.Add( ent ) )
				{
					Selected.Add( ent );
					foreach ( Entity p in ent.Children )
						entsToCheck.Push( p );
					if ( ent.Parent.IsValid() )
						entsToCheck.Push( ent.Parent );

				}
			}
		}

		DuplicatorData Selected = new DuplicatorData();
		Matrix Origin;
		float PasteRotationOffset = 0;
		float PasteHeightOffset = 0;

		[ClientRpc]
		void SetupGhosts( DuplicatorData entities, Vector3 origin )
		{

		}

		void Copy( TraceResult tr )
		{
			var floorTr = Trace.Ray( tr.EndPos, tr.EndPos + new Vector3( 0, 0, -1e6f ) ).WorldOnly().Run();
			Origin = Matrix.CreateTranslation( floorTr.Hit ? floorTr.EndPos : tr.EndPos );

			PasteRotationOffset = 0;
			PasteHeightOffset = 0;
			Selected.Clear();

			if ( AreaCopy )
			{
				foreach ( Entity ent in Physics.GetEntitiesInBox( new BBox( new Vector3( -AreaSize ), new Vector3( AreaSize ) ) ) )
					Selected.Add( ent );
			}
			else
			{
				// Hit an entity
				if ( tr.Entity.IsValid() )
				{
					GetAttachedEntities( tr.Entity );
				}
				else // Hit the world
				{

				}
			}
			SetupGhosts( To.Single( Owner ), Selected, Origin.Transform( new Vector3() ) );
		}

		void Paste( TraceResult tr )
		{

		}

		bool AreaCopy = false;
		void OnTool( InputButton input )
		{
			var startPos = Owner.EyePos;
			var dir = Owner.EyeRot.Forward;
			var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance ).Ignore( Owner ).Run();

			switch ( input )
			{
				case InputButton.Attack1:
					if ( tr.Hit )
					{
						Paste( tr );
						CreateHitEffects( tr.EndPos, tr.Normal );
					}
					break;

				case InputButton.Attack2:
					if ( tr.Hit && tr.Entity.IsValid() )
					{
						Copy( tr );
						CreateHitEffects( tr.EndPos, tr.Normal );
					}
					break;
			}
		}

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( InputButton.Attack1 ) )
					OnTool( InputButton.Attack1 );
				if ( Input.Pressed( InputButton.Attack2 ) )
				{
					if ( Input.Down( InputButton.Run ) )
					{
						AreaCopy = !AreaCopy;
					}
					else
					{
						OnTool( InputButton.Attack2 );
					}
				}
				if ( Input.Pressed( InputButton.Next ) && Input.Down( InputButton.Use ) )
				{
					PasteHeightOffset += 5;
				}
				if ( Input.Pressed( InputButton.Prev ) && Input.Down( InputButton.Use ) )
				{
					PasteHeightOffset -= 5;
				}
			}
		}

		public override void Activate()
		{
			base.Activate();
			if ( Host.IsClient )
			{
				//SpawnMenu.Instance?.ToolPanel?.AddChild( fileSelector );
			}
		}
	}
}
