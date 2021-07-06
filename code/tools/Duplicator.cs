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
		public virtual List<object> PreDuplicatorCopy() { return new List<object>(); }

		// Called after the duplicator has finished creating this entity
		public virtual void PostDuplicatorPaste( List<object> userdata ) { }

		// Called after all entities are created
		public virtual void PostDuplicatorPasteEntities( Dictionary<int, Entity> entities ) { }

		// Called after pasting is done
		public virtual void PostDuplicatorPasteDone() { }

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
						return new DecoderV0( bn ).Decode();
					default:
						throw new Exception( "Invalid encoder version: " + ver );
				}
			}
		}

		static byte[] Encode( DuplicatorData entityData )
		{
			using ( var stream = new MemoryStream() )
			{
				using ( var bn = new BinaryWriter( stream ) )
				{
					new Encoder( bn ).Encode( entityData );
				}
				return stream.GetBuffer();
			}
		}

		private class Encoder
		{
			BinaryWriter bn;
			public Encoder( BinaryWriter bn_ ) { bn = bn_; }
			public void Encode( DuplicatorData entityData )
			{
				bn.Write( (uint)0x44555045 ); // File type 'DUPE'
				bn.Write( (byte)0 ); // Encoder version
				writeString( entityData.name );
				writeString( entityData.author );
				writeString( entityData.date );

				bn.Write( (uint)entityData.entities.Count );
				foreach ( DuplicatorData.DuplicatorItem item in entityData.entities )
				{
					writeEntity( item );
				}
				bn.Write( (uint)entityData.constraints.Count );
				foreach ( DuplicatorData.DuplicatorConstraint item in entityData.constraints )
				{
					writeConstraint( item );
				}
			}
			void writeString( string s )
			{
				bn.Write( s.Length ); bn.Write( Encoding.ASCII.GetBytes( s ) );
			}
			void writeVector( Vector3 v )
			{
				bn.Write( v.x ); bn.Write( v.y ); bn.Write( v.z );
			}
			void writeRotation( Rotation r )
			{
				bn.Write( r.x ); bn.Write( r.y ); bn.Write( r.z ); bn.Write( r.w );
			}
			void writeObject( object o )
			{
				switch ( o )
				{
					case string str:
						bn.Write( (byte)1 );
						writeString( str );
						break;
					case Vector3 v:
						bn.Write( (byte)2 );
						writeVector( v );
						break;
					case Rotation r:
						bn.Write( (byte)3 );
						writeRotation( r );
						break;
					case Entity ent:
						bn.Write( (byte)4 );
						bn.Write( ent.NetworkIdent );
						break;
					default:
						throw new Exception( "Invalid userdata " + o.GetType() );
				}
			}

			void writeEntity( DuplicatorData.DuplicatorItem ent )
			{
				bn.Write( ent.index );
				writeString( ent.className );
				writeString( ent.model );
				writeVector( ent.position );
				writeRotation( ent.rotation );
				bn.Write( ent.userData.Count );
				foreach ( object o in ent.userData ) writeObject( o );
			}

			void writeConstraint( DuplicatorData.DuplicatorConstraint constr )
			{
			}
		}

		private class DecoderV0
		{
			BinaryReader bn;
			public DecoderV0( BinaryReader bn_ ) { bn = bn_; }
			public DuplicatorData Decode()
			{
				DuplicatorData ret = new DuplicatorData();
				ret.name = readString();
				ret.author = readString();
				ret.date = readString();
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 2048 ); i < end; ++i )
				{
					ret.entities.Add( readEntity() );
				}
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 2048 ); i < end; ++i )
				{
					ret.constraints.Add( readConstraint() );
				}
				return ret;
			}
			protected string readString()
			{
				return Encoding.ASCII.GetString( bn.ReadBytes( bn.ReadInt32() ) );
			}
			protected Vector3 readVector()
			{
				return new Vector3( bn.ReadSingle(), bn.ReadSingle(), bn.ReadSingle() ); // Args eval left to right in C#
			}
			protected Rotation readRotation()
			{
				Rotation ret = new Rotation();
				ret.x = bn.ReadSingle(); ret.y = bn.ReadSingle(); ret.z = bn.ReadSingle(); ret.w = bn.ReadSingle();
				return ret;
			}
			protected object readObject()
			{
				byte type = bn.ReadByte();
				switch ( type )
				{
					case 1:
						return readString();
					case 2:
						return readVector();
					case 3:
						return readRotation();
					case 4:
						return bn.ReadInt32();
					default:
						throw new Exception( "Invalid userdata type (" + type + ")" );
				}
			}
			protected DuplicatorData.DuplicatorItem readEntity()
			{
				DuplicatorData.DuplicatorItem ret = new DuplicatorData.DuplicatorItem();
				ret.index = bn.ReadInt32();
				ret.className = readString();
				ret.model = readString();
				ret.position = readVector();
				ret.rotation = readRotation();
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 1024 ); i < end; ++i )
				{
					ret.userData.Add( readObject() );
				}
				return ret;
			}
			protected DuplicatorData.DuplicatorConstraint readConstraint()
			{
				DuplicatorData.DuplicatorConstraint ret = new DuplicatorData.DuplicatorConstraint();
				return ret;
			}
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
			public int index;
			public string className;
			public string model;
			public Vector3 position;
			public Rotation rotation;
			public List<object> userData = new List<object>();
			public DuplicatorItem() { }
			public DuplicatorItem( Entity ent, Matrix origin )
			{
				index = ent.NetworkIdent;
				className = ent.ClassInfo.Name;
				if ( ent is Prop p )
					model = p.GetModel().Name;
				else
					model = "";
				position = origin.Inverted.Transform( ent.Position );
				rotation = ent.Rotation;
				if ( ent is Duplicatable dupe )
					userData = dupe.PreDuplicatorCopy();
			}

			public Entity Spawn( Matrix origin )
			{
				Entity ent = Library.Create<Entity>( className );
				ent.Position = origin.Transform( position );
				ent.Rotation = rotation;
				//ent.PhysicsEnabled = ;
				//ent.EnableSolidCollisions =;
				//ent.Massless = ;
				if ( ent is Duplicatable dupe )
					dupe.PostDuplicatorPaste( userData );
				return ent;
			}
		}
		public class DuplicatorConstraint
		{
			public int entIndex1;
			public int entIndex2;
			public int bone1;
			public int bone2;
			public string type;
			public DuplicatorConstraint()
			{

			}

			public void Spawn( Dictionary<int, Entity> spawnedEnts )
			{

			}
		}

		public string name = "";
		public string author = "";
		public string date = "";
		public List<DuplicatorItem> entities = new List<DuplicatorItem>();
		public List<DuplicatorConstraint> constraints = new List<DuplicatorConstraint>();
		public void Clear() { name = ""; author = ""; date = ""; entities.Clear(); constraints.Clear(); }
		public void Add( Entity ent, Matrix origin )
		{
			entities.Add( new DuplicatorItem( ent, origin ) );
		}
		public void Paste( Matrix origin )
		{
			Dictionary<int, Entity> spawnedEnts = new Dictionary<int, Entity>();
			foreach ( DuplicatorItem item in entities )
			{
				try
				{
					spawnedEnts[item.index] = item.Spawn( origin );
				}
				catch ( Exception e )
				{
					Log.Warning( e, "Failed to spawn class (" + item.className + ")" );
				}
			}
			foreach ( Entity ent in spawnedEnts.Values )
			{
				if ( ent is Duplicatable dupe )
					dupe.PostDuplicatorPasteEntities( spawnedEnts );
			}
			foreach ( DuplicatorConstraint item in constraints )
			{
				try
				{
					item.Spawn( spawnedEnts );
				}
				catch ( Exception e )
				{
					Log.Warning( e, "Failed to apply constraint" );
				}
			}
			foreach ( Entity ent in spawnedEnts.Values )
			{
				if ( ent is Duplicatable dupe )
					dupe.PostDuplicatorPasteDone();
			}
		}
	}
}

namespace Sandbox.Tools
{
	[Library( "tool_duplicator", Title = "Duplicator", Description = "Save and load contraptions", Group = "construction" )]
	public partial class DuplicatorTool : BaseTool
	{
		// Default behavior will be restoring the freeze state of entities to what they were when copied
		[ConVar.ClientData( "tool_duplicator_freeze_all", Help = "Freeze all entities during pasting", Saved = true )]
		public bool FreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_unfreeze_all", Help = "Unfreeze all entities after pasting", Saved = true )]
		public bool UnfreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_area_size", Help = "Area copy size", Saved = true )]
		public float AreaSize { get; set; } = 250;



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
					Selected.Add( ent, Origin );
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

		List<PreviewEntity> ghosts = new List<PreviewEntity>();
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
					Selected.Add( ent, Origin );
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

		static HashSet<string> AllowedClasses = new HashSet<string>();
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
