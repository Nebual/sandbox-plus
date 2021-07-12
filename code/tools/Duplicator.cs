using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				if ( bn.ReadUInt32() != 0x45505544 ) throw new Exception( "The file isn't a duplicator file!" );
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

		public static byte[] Encode( DuplicatorData entityData )
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
				bn.Write( (uint)0x45505544 ); // File type 'DUPE'
				bn.Write( (byte)0 ); // Encoder version
				writeString( entityData.name );
				writeString( entityData.author );
				writeString( entityData.date );

				bn.Write( (uint)entityData.entities.Count );
				foreach ( DuplicatorData.DuplicatorItem item in entityData.entities )
				{
					writeEntity( item );
				}
				bn.Write( (uint)entityData.joints.Count );
				foreach ( DuplicatorData.DuplicatorConstraint item in entityData.joints )
				{
					writeConstraint( item );
				}
			}
			void writeString( string s )
			{
				byte[] bytes = Encoding.ASCII.GetBytes( s );
				bn.Write( bytes.Length ); bn.Write( bytes );
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
					ret.joints.Add( readConstraint() );
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
			return JsonSerializer.Serialize( entityData, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true } );
		}

		static DuplicatorData DecodeJsonV0( string data )
		{
			return (DuplicatorData)JsonSerializer.Deserialize( data, typeof( DuplicatorData ) );
		}
	}

	public class DuplicatorData
	{
		public static HashSet<string> AllowedClasses = new HashSet<string>()
		{
			"prop_physics"
		};

		public class DuplicatorItem
		{
			public int index;
			public string className;
			public string model;
			public Vector3 position;
			public Rotation rotation;
			public List<object> userData = new List<object>();
			public DuplicatorItem() { }
			public DuplicatorItem( Entity ent, Transform origin )
			{
				index = ent.NetworkIdent;
				className = ent.ClassInfo.Name;
				if ( ent is Prop p )
					model = p.GetModel().Name;
				else
					model = "";
				position = origin.PointToLocal( ent.Position );
				rotation = origin.RotationToLocal( ent.Rotation );
				if ( ent is Duplicatable dupe )
					userData = dupe.PreDuplicatorCopy();
			}

			public Entity Spawn( Transform origin )
			{
				Entity ent = Library.Create<Entity>( className );
				ent.Position = origin.PointToWorld( position );
				ent.Rotation = origin.RotationToWorld( rotation );
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
			public Vector3 anchor1;
			public Vector3 anchor2;
			public int entIndex1;
			public int entIndex2;
			public int bone1;
			public int bone2;
			public string type;
			public DuplicatorConstraint() { }
			public DuplicatorConstraint( PhysicsJoint joint )
			{
				anchor1 = joint.Anchor1;
				anchor2 = joint.Anchor2;
				entIndex1 = joint.Body1.Entity.NetworkIdent;
				entIndex2 = joint.Body2.Entity.NetworkIdent;
			}

			public void Spawn( Dictionary<int, Entity> spawnedEnts )
			{

			}
		}

		public string name = "";
		public string author = "";
		public string date = "";
		public List<DuplicatorItem> entities = new List<DuplicatorItem>();
		public List<DuplicatorConstraint> joints = new List<DuplicatorConstraint>();
		public void Add( Entity ent, Transform origin )
		{
			if ( ent is Duplicatable || AllowedClasses.Contains( ent.ClassInfo.Name ) )
				entities.Add( new DuplicatorItem( ent, origin ) );
		}
		public void Add( PhysicsJoint joint )
		{
			joints.Add( new DuplicatorConstraint( joint ) );
		}
		public List<DuplicatorGhost> getGhosts()
		{
			List<DuplicatorGhost> ret = new();
			foreach(DuplicatorItem item in entities)
			{
				ret.Add( new DuplicatorGhost( item.position, item.rotation, item.model ) );
			}
			return ret;
		}
	}

	public class DuplicatorPasteJob
	{
		Player owner;
		DuplicatorData data;
		Transform origin;
		Stopwatch timeUsed = new Stopwatch();
		Stopwatch timeElapsed = new Stopwatch();
		Dictionary<int, Entity> entList = new Dictionary<int, Entity>();
		public DuplicatorPasteJob( Player owner_, DuplicatorData data_, Transform origin_ )
		{
			owner = owner_;
			data = data_;
			origin = origin_;
			timeElapsed.Start();
			Event.Register( this );
		}

		bool checkTime()
		{
			return timeUsed.Elapsed.TotalSeconds / timeElapsed.Elapsed.TotalSeconds < 0.1; // Stay under 10% cputime
		}

		int spawnedEnts = 0;
		int spawnedConstraints = 0;
		bool next()
		{
			if ( spawnedEnts < data.entities.Count )
			{
				DuplicatorData.DuplicatorItem item = data.entities[spawnedEnts++];
				try
				{
					entList[item.index] = item.Spawn( origin );
				}
				catch ( Exception e )
				{
					Log.Warning( e, "Failed to spawn class (" + item.className + ")" );
				}
				if ( spawnedEnts == data.entities.Count )
				{
					foreach ( Entity ent in entList.Values )
					{
						if ( ent is Duplicatable dupe )
							dupe.PostDuplicatorPasteEntities( entList );
					}
				}
				return true;
			}
			else if ( spawnedConstraints < data.joints.Count )
			{
				DuplicatorData.DuplicatorConstraint item = data.joints[spawnedConstraints++];
				try
				{
					item.Spawn( entList );
				}
				catch ( Exception e )
				{
					Log.Warning( e, "Failed to spawn constraint (" + item.type + ")" );
				}
				return true;
			}
			else
			{
				foreach ( Entity ent in entList.Values )
				{
					if ( ent is Duplicatable dupe )
						dupe.PostDuplicatorPasteDone();
				}
				return false;
			}
		}

		[Event.Tick]
		public void Tick()
		{
			timeUsed.Start();
			while ( checkTime() )
			{
				if ( !next() )
				{
					Tools.DuplicatorTool.Pasting.Remove( owner );
					Event.Unregister( this );
					return;
				}
			}
			timeUsed.Stop();
		}
	}

	public struct DuplicatorGhost
	{
		public Vector3 position;
		public Rotation rotation;
		public string model;
		public DuplicatorGhost(Vector3 pos, Rotation rot, string model_)
		{
			position = pos;
			rotation = rot;
			model = model_;
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

		public static Dictionary<Player, DuplicatorPasteJob> Pasting = new Dictionary<Player, DuplicatorPasteJob>();

		void GetAttachedEntities( Entity baseEnt, List<Entity> ents, List<PhysicsJoint> joints )
		{
			HashSet<Entity> entsChecked = new();
			HashSet<PhysicsJoint> jointsChecked = new();
			Stack<Entity> entsToCheck = new();
			entsChecked.Add( baseEnt );
			entsToCheck.Push( baseEnt );

			while ( entsToCheck.Count > 0 )
			{
				Entity ent = entsToCheck.Pop();
				foreach ( Entity e in ent.Children )
				{
					if ( entsChecked.Add( e ) )
					{
						entsToCheck.Push( e );
					}
				}

				if ( ent.Parent.IsValid() && entsChecked.Add( ent.Parent ) )
				{
					entsToCheck.Push( ent.Parent );
				}

				if ( ent.PhysicsGroup is not null )
				{
					for ( int i = 0, end = ent.PhysicsGroup.BodyCount; i < end; ++i )
					{
						Entity e = ent.PhysicsGroup.GetBody( i ).Entity;
						if ( entsChecked.Add( e ) )
							entsToCheck.Push( e );
					}
					foreach(PhysicsJoint j in GetJoints(ent))
					{
						if( jointsChecked.Add(j) )
						{
							if ( entsChecked.Add( j.Body1.Entity ) )
								entsToCheck.Push( j.Body1.Entity );
							if ( entsChecked.Add( j.Body2.Entity ) )
								entsToCheck.Push( j.Body2.Entity );
						}
					}
				}
			}
			ents.AddRange( entsChecked );
			joints.AddRange( jointsChecked );
		}

		List<PhysicsJoint> GetJoints( Entity ent )
		{
			List<PhysicsJoint> ret = new();
			PhysicsGroup group = ent.PhysicsGroup;
			if ( group is not null )
			{
				for ( int i = 0, end = group.JointCount; i < end; ++i )
				{
					ret.Add( group.GetJoint( i ) );
				}
			}
			return ret;
		}
		List<PhysicsJoint> GetJoints( List<Entity> ents )
		{
			HashSet<PhysicsJoint> jointsChecked = new();
			foreach ( Entity ent in ents )
			{
				PhysicsGroup group = ent.PhysicsGroup;
				if ( group is not null )
				{
					for ( int i = 0, end = group.JointCount; i < end; ++i )
					{
						jointsChecked.Add( group.GetJoint( i ) );
					}
				}
			}
			return jointsChecked.ToList();
		}

		DuplicatorData Selected = null;
		[Net, Predicted] float PasteRotationOffset { get; set; } = 0;
		[Net, Predicted] float PasteHeightOffset { get; set; } = 0;
		[Net, Predicted] bool AreaCopy { get; set; } = false;
		[Net, Predicted] Transform Origin { get; set; }

		static DuplicatorTool getTool( Entity player )
		{
			if ( player == null ) return null;
			var inventory = player.Inventory;
			if ( inventory == null ) return null;
			if ( inventory.Active is not Tool tool ) return null;
			if ( tool == null ) return null;
			if ( tool.CurrentTool is not DuplicatorTool dupe ) return null;
			return dupe;
		}

		[ClientRpc]
		public static void SetupGhostsRpc( List<DuplicatorGhost> ghosts )
		{
			getTool( Local.Pawn )?.SetupGhosts( ghosts );
		}
		public void SetupGhosts( List<DuplicatorGhost> ghosts )
		{
			foreach ( DuplicatorGhost ghost in ghosts )
			{
				PreviewEntity previewModel = null;
				if ( TryCreatePreview( ref previewModel, ghost.model ) )
				{
					previewModel.RelativeToNormal = false;
					previewModel.OffsetBounds = true;
					previewModel.PositionOffset = -previewModel.CollisionBounds.Center;
				}
			}
		}

		[ClientCmd( "tool_duplicator_openfile", Help = "Loads a duplicator file" )]
		static void OpenFile( string path )
		{
			ReceiveDuplicatorDataCmd( FileSystem.Data.ReadAllText( path ) );
		}

		[ClientCmd( "tool_duplicator_savefile", Help = "Saves a duplicator file" )]
		static void SaveFile( string path )
		{
			SaveDuplicatorDataCmd( path );
		}

		[ClientRpc]
		public static void SaveFileData(string path, byte[] data)
		{
			using (Stream s = FileSystem.Data.OpenWrite( path ))
			{
				s.Write( data, 0, data.Length );
			}
		}

		[ServerCmd]
		static void SaveDuplicatorDataCmd( string path )
		{
			getTool( ConsoleSystem.Caller.Pawn )?.SaveDuplicatorData( path );
		}
		[ServerCmd]
		static void ReceiveDuplicatorDataCmd( string data )
		{
			getTool( ConsoleSystem.Caller.Pawn )?.ReceiveDuplicatorData( data );
		}

		void ReceiveDuplicatorData( string data )
		{
			try
			{
				Selected = DuplicatorEncoder.Decode( Encoding.ASCII.GetBytes( data ) );
				// Ghosts are set up on the client already when they load the file
			}
			catch
			{
				// Reset and clear the ghosts
				Selected = null;
				SetupGhostsRpc( To.Single( Owner ), new DuplicatorData().getGhosts() );
			}
		}
		void SaveDuplicatorData( string path )
		{
			if ( Selected is null ) return;
			try
			{
				byte[] data = DuplicatorEncoder.Encode( Selected );
				SaveFileData( To.Single( Owner ), path, data );
			}
			catch
			{
			}
		}

		void Copy( TraceResult tr )
		{
			DuplicatorData copied = new DuplicatorData();

			var floorTr = Trace.Ray( tr.EndPos, tr.EndPos + new Vector3( 0, 0, -1e6f ) ).WorldOnly().Run();
			Transform origin = new Transform( floorTr.Hit ? floorTr.EndPos : tr.EndPos );
			PasteRotationOffset = 0;
			PasteHeightOffset = 0;

			if ( AreaCopy )
			{
				AreaCopy = false;
				List<Entity> ents = new List<Entity>();
				foreach ( Entity ent in Physics.GetEntitiesInBox( new BBox( new Vector3( -AreaSize ), new Vector3( AreaSize ) ) ) )
				{
					ents.Add( ent );
					copied.Add( ent, origin );
				}
				foreach ( PhysicsJoint j in GetJoints( ents ) )
					copied.Add( j );
			}
			else
			{
				if ( tr.Entity.IsValid() )
				{
					List<Entity> ents = new();
					List<PhysicsJoint> joints = new();
					GetAttachedEntities( tr.Entity, ents, joints );
					foreach ( Entity e in ents )
						copied.Add( e, origin );
					foreach ( PhysicsJoint j in joints )
						copied.Add( j );
				}
				else
				{
					if ( Selected is null )
					{
						// Select all entities you own
					}
				}
			}

			SetupGhostsRpc( To.Single( Owner ), copied.getGhosts() );
			Selected = copied.entities.Count > 0 ? copied : null;
		}

		void Paste( TraceResult tr )
		{
			Pasting[Owner] = new DuplicatorPasteJob( Owner, Selected, new Transform( tr.EndPos + new Vector3( 0, 0, PasteHeightOffset ) ) );
		}

		void OnTool( InputButton input )
		{
			if ( Pasting.ContainsKey( Owner ) ) return;

			var startPos = Owner.EyePos;
			var dir = Owner.EyeRot.Forward;
			var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance ).Ignore( Owner ).Run();

			switch ( input )
			{
				case InputButton.Attack1:
					if ( tr.Hit && Selected is not null )
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
			if ( Input.Down( InputButton.Use ) )
			{
				PasteRotationOffset += Input.MouseDelta[0];
				Input.MouseDelta = new Vector3();
			}
			if ( Input.Pressed( InputButton.Attack2 ) && Input.Down( InputButton.Run ) )
			{
				AreaCopy = !AreaCopy;
			}
			if ( Input.Pressed( InputButton.Next ) && Input.Down( InputButton.Use ) )
			{
				PasteHeightOffset += 5;
			}
			if ( Input.Pressed( InputButton.Prev ) && Input.Down( InputButton.Use ) )
			{
				PasteHeightOffset -= 5;
			}

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
