using Sandbox.Physics;
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
	public interface IDuplicatable
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
		private static byte LeftBracket = 123;
		public static DuplicatorData Decode( byte[] data )
		{
			if ( data[0] == LeftBracket )
			{
				// not working yet
				// return DecodeJsonV0( Encoding.ASCII.GetString( data ) );
			}
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
				return stream.ToArray();
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
			void writeTransform( Transform t )
			{
				writeVector( t.Position );
				writeRotation( t.Rotation );
				bn.Write( t.Scale );
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
				bn.Write( ent.frozen );
				bn.Write( ent.userData.Count );
				foreach ( object o in ent.userData ) writeObject( o );
			}

			void writeConstraint( DuplicatorData.DuplicatorConstraint constr )
			{
				bn.Write( (byte)constr.type );
				bn.Write( constr.entIndex1 );
				bn.Write( constr.entIndex2 );
				bn.Write( constr.bone1 );
				bn.Write( constr.bone2 );
				writeTransform( constr.anchor1 );
				writeTransform( constr.anchor2 );
				bn.Write( constr.collisions );
				bn.Write( constr.enableAngularConstraint );
				bn.Write( constr.enableLinearConstraint );
				switch ( constr.type )
				{
					case Tools.ConstraintType.Spring:
						bn.Write( constr.maxLength );
						bn.Write( constr.minLength );
						writeVector( constr.springLinear );
						break;
					case Tools.ConstraintType.Axis:
						bn.Write( constr.minAngle );
						bn.Write( constr.maxAngle );
						break;
					case Tools.ConstraintType.Slider:
						bn.Write( constr.maxLength );
						bn.Write( constr.minLength );
						break;
				}
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
			protected Transform readTransform()
			{
				return new Transform( readVector(), readRotation(), bn.ReadSingle() );
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
				ret.frozen = bn.ReadBoolean();
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 1024 ); i < end; ++i )
				{
					ret.userData.Add( readObject() );
				}
				return ret;
			}
			protected DuplicatorData.DuplicatorConstraint readConstraint()
			{
				DuplicatorData.DuplicatorConstraint ret = new DuplicatorData.DuplicatorConstraint();
				ret.type = (Tools.ConstraintType)bn.ReadByte();
				ret.entIndex1 = bn.ReadInt32();
				ret.entIndex2 = bn.ReadInt32();
				ret.bone1 = bn.ReadInt32();
				ret.bone2 = bn.ReadInt32();
				ret.anchor1 = readTransform();
				ret.anchor2 = readTransform();
				ret.collisions = bn.ReadBoolean();
				ret.enableAngularConstraint = bn.ReadBoolean();
				ret.enableLinearConstraint = bn.ReadBoolean();
				switch ( ret.type )
				{
					case Tools.ConstraintType.Spring:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						ret.springLinear = (PhysicsSpring)readVector();
						break;
					case Tools.ConstraintType.Axis:
						ret.minAngle = bn.ReadSingle();
						ret.maxAngle = bn.ReadSingle();
						break;
					case Tools.ConstraintType.Slider:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						break;
				}
				return ret;
			}
		}

		public static string EncodeJson( DuplicatorData entityData )
		{
			return JsonSerializer.Serialize( entityData, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true } );
		}

		public static DuplicatorData DecodeJsonV0( string data )
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
			public bool frozen;
			public List<object> userData = new List<object>();
			public DuplicatorItem() { }
			public DuplicatorItem( Entity ent, Transform origin )
			{
				index = ent.NetworkIdent;
				className = ent.ClassName;
				if ( ent is ModelEntity p )
					model = p.Model.Name;
				else
					model = "";
				position = origin.PointToLocal( ent.Position );
				rotation = origin.RotationToLocal( ent.Rotation );
				frozen = ent.PhysicsGroup?.GetBody( 0 )?.BodyType == PhysicsBodyType.Static;
				if ( ent is IDuplicatable dupe )
					userData = dupe.PreDuplicatorCopy();
			}

			public Entity Spawn( Transform origin )
			{
				ModelEntity ent = TypeLibrary.Create<ModelEntity>( className );
				ent.Position = origin.PointToWorld( position );
				ent.Rotation = origin.RotationToWorld( rotation );
				ent.SetModel( model );
				ent.PhysicsEnabled = false;
				if ( frozen )
				{
					ent.PhysicsBody.BodyType = PhysicsBodyType.Static;
				}
				//ent.EnableSolidCollisions =;
				//ent.Massless = ;
				if ( ent is IDuplicatable dupe )
					dupe.PostDuplicatorPaste( userData );
				return ent;
			}
		}
		public class DuplicatorConstraint
		{
			public Tools.ConstraintType type;
			public int entIndex1;
			public int entIndex2;
			public int bone1;
			public int bone2;
			public Transform anchor1;
			public Transform anchor2;
			public bool collisions;
			public bool enableAngularConstraint;
			public bool enableLinearConstraint;

			// spring
			public float maxLength;
			public float minLength;
			public PhysicsSpring springLinear;
			// hinge
			public float minAngle;
			public float maxAngle;
			// public float friction; // no getter :(

			public DuplicatorConstraint() { }
			public DuplicatorConstraint( PhysicsJoint joint )
			{
				anchor1 = joint.Point1.LocalTransform;
				anchor2 = joint.Point2.LocalTransform;
				entIndex1 = joint.Body1.GetEntity().NetworkIdent;
				entIndex2 = joint.Body2.GetEntity().NetworkIdent;
				collisions = joint.Collisions;
				enableAngularConstraint = joint.EnableAngularConstraint;
				enableLinearConstraint = joint.EnableLinearConstraint;
				if ( joint is FixedJoint fixedJoint )
				{
					type = Tools.ConstraintType.Weld;
				}
				else if ( joint is SpringJoint springJoint )
				{
					type = Tools.ConstraintType.Spring;
					maxLength = springJoint.MaxLength;
					minLength = springJoint.MinLength;
					springLinear = springJoint.SpringLinear;
				}
				else if ( joint is HingeJoint hingeJoint )
				{
					type = Tools.ConstraintType.Axis;
					minAngle = hingeJoint.MinAngle;
					maxAngle = hingeJoint.MaxAngle;
				}
				else if ( joint is BallSocketJoint ballJoint )
				{
					type = Tools.ConstraintType.BallSocket;
				}
				else if ( joint is SliderJoint sliderJoint )
				{
					type = Tools.ConstraintType.Slider;
					maxLength = sliderJoint.MaxLength;
					minLength = sliderJoint.MinLength;
				}
				else
				{
					Log.Warning( $"Duplicator: Unknown joint type {joint.GetType()}" );
					return;
				}
			}

			public void Spawn( Dictionary<int, Entity> spawnedEnts )
			{
				var ent1 = spawnedEnts[entIndex1];
				var ent2 = spawnedEnts[entIndex2];
				var body1 = ent1.PhysicsGroup.GetBody( 0 );
				var body2 = ent2.PhysicsGroup.GetBody( 0 );
				var point1 = PhysicsPoint.Local( body1, anchor1.Position, anchor1.Rotation );
				var point2 = PhysicsPoint.Local( body2, anchor2.Position, anchor2.Rotation );
				PhysicsJoint joint;
				if ( type == Tools.ConstraintType.Weld || type == Tools.ConstraintType.Nocollide )
				{
					joint = PhysicsJoint.CreateFixed(
						point1,
						point2
					);
				}
				else if ( type == Tools.ConstraintType.Spring )
				{
					joint = PhysicsJoint.CreateSpring(
						point1,
						point2,
						minLength,
						maxLength
					);
					((SpringJoint)joint).SpringLinear = springLinear;
					var rope = MakeRope( point1, point2 );
					joint.OnBreak += () => { rope?.Destroy( true ); };
				}
				else if ( type == Tools.ConstraintType.Axis )
				{
					joint = PhysicsJoint.CreateHinge(
						 body1,
						 body2,
						anchor1.Position,
						anchor1.Rotation.Up
					);
					((HingeJoint)joint).MinAngle = minAngle;
					((HingeJoint)joint).MaxAngle = maxAngle;
				}
				else if ( type == Tools.ConstraintType.BallSocket )
				{
					joint = PhysicsJoint.CreateBallSocket(
						 body1,
						 body2,
						anchor1.Position
					);
				}
				else if ( type == Tools.ConstraintType.Slider )
				{
					joint = PhysicsJoint.CreateSlider(
						 point1,
						 point2,
						minLength,
						maxLength
					);
					var rope = MakeRope( point1, point2 );
					joint.OnBreak += () => { rope?.Destroy( true ); };
				}
				else
				{
					Log.Warning( $"Duplicator: Unknown joint type {type}" );
					return;
				}
				joint.Collisions = collisions;
				joint.EnableAngularConstraint = enableAngularConstraint;
				joint.EnableLinearConstraint = enableLinearConstraint;
				Event.Run( "joint.spawned", joint, (Player)null );
			}
		}

		private static Particles MakeRope( PhysicsPoint point1, PhysicsPoint point2 )
		{
			var rope = Particles.Create( "particles/rope.vpcf" );
			rope.SetEntity( 0, point1.Body.GetEntity(), point1.LocalPosition );
			rope.SetEntity( 1, point2.Body.GetEntity(), point2.LocalPosition );
			return rope;
		}

		public string name = "";
		public string author = "";
		public string date = "";
		public List<DuplicatorItem> entities = new List<DuplicatorItem>();
		public List<DuplicatorConstraint> joints = new List<DuplicatorConstraint>();
		public void Add( Entity ent, Transform origin )
		{
			if ( ent is IDuplicatable || AllowedClasses.Contains( ent.ClassName ) )
				entities.Add( new DuplicatorItem( ent, origin ) );
		}
		public void Add( PhysicsJoint joint )
		{
			joints.Add( new DuplicatorConstraint( joint ) );
		}
		public List<DuplicatorGhost> getGhosts()
		{
			List<DuplicatorGhost> ret = new();
			foreach ( DuplicatorItem item in entities )
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
		Dictionary<int, DuplicatorData.DuplicatorItem> entData = new();
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
					entData[item.index] = item;
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
						if ( ent is IDuplicatable dupe )
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
				foreach ( int index in entList.Keys )
				{
					Entity ent = entList[index];
					if ( ent is IDuplicatable dupe )
						dupe.PostDuplicatorPasteDone();
					if ( ent is ModelEntity modelEnt )
					{
						if ( !entData[index].frozen )
						{
							// Enable physics after all entities are spawned, except for saved-as-frozen ents
							modelEnt.PhysicsEnabled = true;
						}
					}
				}

				Event.Run( "undo.add", () =>
				{
					foreach ( Entity ent in entList.Values )
					{
						ent.Delete();
					}
					return $"Removed duplicator paste {data.name}";
				}, owner );
				return false;
			}
		}

		[GameEvent.Tick]
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
		public DuplicatorGhost( Vector3 pos, Rotation rot, string model_ )
		{
			position = pos;
			rotation = rot;
			model = model_;
		}
	}
}

namespace Sandbox.Tools
{
	[Library( "tool_duplicator", Title = "Duplicator", Description =
@"Save and load contraptions
Primary: Paste contraption
Secondary: Copy contraption (shift for area copy)", Group = "construction" )]
	public partial class DuplicatorTool : BaseTool
	{
		// Default behavior will be restoring the freeze state of entities to what they were when copied
		[ConVar.ClientData( "tool_duplicator_freeze_all", Help = "Freeze all entities during pasting", Saved = true )]
		public static bool FreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_unfreeze_all", Help = "Unfreeze all entities after pasting", Saved = true )]
		public static bool UnfreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_area_size", Help = "Area copy size", Saved = true )]
		public static float AreaSize { get; set; } = 250;

		public static Dictionary<Player, DuplicatorPasteJob> Pasting = new();

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
						Entity e = ent.PhysicsGroup.GetBody( i ).GetEntity();
						if ( entsChecked.Add( e ) )
							entsToCheck.Push( e );
					}
					foreach ( PhysicsJoint j in GetJoints( ent ) )
					{
						if ( jointsChecked.Add( j ) )
						{
							if ( entsChecked.Add( j.Body1.GetEntity() ) )
								entsToCheck.Push( j.Body1.GetEntity() );
							if ( entsChecked.Add( j.Body2.GetEntity() ) )
								entsToCheck.Push( j.Body2.GetEntity() );
						}
					}
				}
			}
			ents.AddRange( entsChecked );
			joints.AddRange( jointsChecked );
		}

		static List<PhysicsJoint> GetJoints( Entity ent )
		{
			var joints = new List<PhysicsJoint>();
			if ( ent.PhysicsGroup is not null )
			{
				joints.AddRange( ent.PhysicsGroup.Joints );
			}

			// ent.PhysicsGroup.Joints only appears to work for ModelDoc joints (eg. within a ragdoll), not for PhysicsJoint.Create
			var jointTracker = ent.Components.Get<JointTrackerComponent>();
			if ( jointTracker is not null )
			{
				joints.AddRange( jointTracker.Joints );
			}
			return joints;
		}
		static List<PhysicsJoint> GetJoints( List<Entity> ents )
		{
			HashSet<PhysicsJoint> jointsChecked = new();
			foreach ( Entity ent in ents )
			{
				if ( ent.PhysicsGroup is not null )
				{
					jointsChecked.UnionWith( ent.PhysicsGroup.Joints );
				}
				var jointTracker = ent.Components.Get<JointTrackerComponent>();
				if ( jointTracker is not null )
				{
					jointsChecked.UnionWith( jointTracker.Joints );
				}
			}
			return jointsChecked.ToList();
		}

		DuplicatorData Selected = null;
		[Net, Predicted] float PasteRotationOffset { get; set; } = 0;
		[Net, Predicted] float PasteHeightOffset { get; set; } = 0;
		[Net, Predicted] bool AreaCopy { get; set; } = false;

		static DuplicatorTool getTool( IEntity player )
		{
			if ( player == null ) return null;
			var inventory = (player as Player).Inventory;
			if ( inventory == null ) return null;
			if ( inventory.Active is not Tool tool ) return null;
			if ( tool == null ) return null;
			if ( tool.CurrentTool is not DuplicatorTool dupe ) return null;
			return dupe;
		}

		void DisplayGhosts( List<DuplicatorGhost> ghosts )
		{
			DeletePreviews();
			foreach ( var ghost in ghosts )
			{
				PreviewEntity previewModel = null;
				if ( TryCreatePreview( ref previewModel, ghost.model ) )
				{
					previewModel.RelativeToNormal = false;
					previewModel.OffsetBounds = false;
					previewModel.PositionOffset = ghost.position;
					previewModel.RotationOffset = ghost.rotation;
				}
			}
		}

		[ConCmd.Client( "tool_duplicator_openfile", Help = "Loads a duplicator file" )]
		static void OpenFile( string path )
		{
			NData.Client.SendToServer( "duplicator", FileSystem.OrganizationData.ReadAllBytes( "dupes/" + path ).ToArray() );
			Analytics.Increment( "duplicator.load" );
		}

		[ConCmd.Client( "tool_duplicator_savefile", Help = "Saves a duplicator file" )]
		static void SaveFile( string path )
		{
			SaveDuplicatorDataCmd( path );
			Analytics.Increment( "duplicator.save" );
		}

		[ClientRpc]
		public static void SaveFileData( string path, byte[] data )
		{
			FileSystem.OrganizationData.CreateDirectory( "dupes" );
			using ( Stream s = FileSystem.OrganizationData.OpenWrite( "dupes/" + path ) )
			{
				s.Write( data, 0, data.Length );
			}
		}

		[ConCmd.Server]
		static void SaveDuplicatorDataCmd( string path )
		{
			getTool( ConsoleSystem.Caller.Pawn )?.SaveDuplicatorData( path );
		}

		[Event( "ndata.received.duplicator" )]
		static void ReceiveDuplicatorDataCmd( IClient client, byte[] data )
		{
			getTool( client.Pawn )?.ReceiveDuplicatorData( data );
		}

		void ReceiveDuplicatorData( byte[] data )
		{
			try
			{
				Selected = DuplicatorEncoder.Decode( data );
				DisplayGhosts( Selected.getGhosts() );
			}
			catch ( Exception e )
			{
				Reset();
				Log.Warning( $"Failed to load duplicator file: {e}" );
			}
		}
		void SaveDuplicatorData( string path )
		{
			if ( Selected is null ) return;
			try
			{
				byte[] data;
				if ( path.EndsWith( ".json" ) )
				{
					data = Encoding.UTF8.GetBytes( DuplicatorEncoder.EncodeJson( Selected ) );
				}
				else
				{
					data = DuplicatorEncoder.Encode( Selected );
				}
				SaveFileData( To.Single( Owner ), path, data );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save duplicator file: {e}" );
			}
		}

		void Copy( TraceResult tr )
		{
			DuplicatorData copied = new()
			{
				author = Owner.Client.Name,
				date = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:sszz" )
			};

			var floorTr = Trace.Ray( tr.EndPosition, tr.EndPosition + new Vector3( 0, 0, -1e6f ) ).StaticOnly().Run();
			Transform origin = new Transform( floorTr.Hit ? floorTr.EndPosition : tr.EndPosition );
			PasteRotationOffset = 0;
			PasteHeightOffset = 0;

			if ( AreaCopy )
			{
				AreaCopy = false;
				var areaSize = new Vector3( int.Parse( GetConvarValue( "tool_duplicator_area_size", "250" ) ) );
				List<Entity> ents = new List<Entity>();
				foreach ( Entity ent in Entity.FindInBox( new BBox( origin.Position - areaSize, origin.Position + areaSize ) ) )
				{
					// todo: should this also grab AttachedEntities?
					ents.Add( ent );
					copied.Add( ent, origin );
				}
				foreach ( PhysicsJoint j in GetJoints( ents ) )
					copied.Add( j );
			}
			else if ( tr.Entity.IsValid() )
			{
				List<Entity> ents = new();
				List<PhysicsJoint> joints = new();
				GetAttachedEntities( tr.Entity, ents, joints );
				foreach ( Entity e in ents )
					copied.Add( e, origin );
				foreach ( PhysicsJoint j in joints )
					copied.Add( j );
			}

			DisplayGhosts( copied.getGhosts() );
			Selected = copied.entities.Count > 0 ? copied : null;
			Analytics.ServerIncrement( To.Single( Owner.Client ), "duplicator.copy" );
		}

		void Paste( TraceResult tr )
		{
			// We can add rotation back in once the ghosts also rotate
			// var modelRotation = Rotation.From( new Angles( 0, Owner.EyeRotation.Angles().yaw, 0 ) );
			Pasting[Owner] = new DuplicatorPasteJob( Owner, Selected, new Transform( tr.EndPosition + new Vector3( 0, 0, PasteHeightOffset ) ) );
			Analytics.ServerIncrement( To.Single( Owner.Client ), "duplicator.paste" );
		}

		void OnTool( string input )
		{
			if ( Pasting.ContainsKey( Owner ) ) return;

			TraceResult tr;
			switch ( input )
			{
				case "attack1":
					tr = DoTrace( false );
					if ( tr.Hit && Selected is not null )
					{
						Paste( tr );
						CreateHitEffects( tr.EndPosition, tr.Normal );
					}
					break;

				case "attack2":
					tr = DoTrace();
					if ( tr.Hit && tr.Entity.IsValid() )
					{
						Copy( tr );
						CreateHitEffects( tr.EndPosition, tr.Normal );
					}
					break;
			}
		}

		public override void Simulate()
		{
			if ( Input.Down( "use" ) )
			{
				PasteRotationOffset += Input.MouseDelta.x;
				Input.MouseDelta = new Vector3();
			}
			if ( Input.Pressed( "attack2" ) && Input.Down( "run" ) )
			{
				AreaCopy = !AreaCopy;
			}
			if ( Input.Pressed( "SlotNext" ) && Input.Down( "use" ) )
			{
				PasteHeightOffset += 5;
			}
			if ( Input.Pressed( "SlotPrev" ) && Input.Down( "use" ) )
			{
				PasteHeightOffset -= 5;
			}

			if ( !Game.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( "attack1" ) )
					OnTool( "attack1" );
				if ( Input.Pressed( "attack2" ) )
					OnTool( "attack2" );
				if ( Input.Pressed( "SlotNext" ) && Input.Down( "use" ) )
				{
					PasteHeightOffset += 5;
				}
				if ( Input.Pressed( "SlotPrev" ) && Input.Down( "use" ) )
				{
					PasteHeightOffset -= 5;
				}
			}
		}

		public void Reset()
		{
			Selected = null;
			DisplayGhosts( new List<DuplicatorGhost>() );
		}

		public override void Activate()
		{
			base.Activate();
			if ( Game.IsClient )
			{
				//SpawnMenu.Instance?.ToolPanel?.AddChild( fileSelector );
			}
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				var toolConfigUi = new DuplicatorToolConfig();
				SpawnMenu.Instance?.ToolPanel?.AddChild( toolConfigUi );
			}
		}
	}
}
