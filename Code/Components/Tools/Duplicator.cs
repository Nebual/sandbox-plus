using Sandbox.Physics;
using SandboxPlus.Tools;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SandboxPlus
{
	public interface IDuplicatable
	{
		// Called while copying to store entity data
		public virtual Dictionary<string, object> PreDuplicatorCopy() { return new(); }

		// Called after the duplicator has finished creating this entity
		public virtual void PostDuplicatorPaste( Dictionary<string, object> data ) { }

		// Called after all entities are created
		public virtual void PostDuplicatorPasteEntities( Dictionary<int, GameObject> entities, Dictionary<string, object> data ) { }

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
				return DecodeJsonV0( Encoding.ASCII.GetString( data ) );
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
					case 1:
						return new DecoderV1( bn ).Decode();
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
				bn.Write( (byte)1 ); // Encoder version
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
			void writeInt( int i )
			{
				bn.Write( i );
			}
			void writeFloat( float f )
			{
				bn.Write( f );
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
			void writeVector4( Vector4 v )
			{
				bn.Write( v.x ); bn.Write( v.y ); bn.Write( v.z ); bn.Write( v.w );
			}
			void writeRotation( Rotation r )
			{
				bn.Write( r.x ); bn.Write( r.y ); bn.Write( r.z ); bn.Write( r.w );
			}
			void writeTransform( Transform t )
			{
				writeVector( t.Position );
				writeRotation( t.Rotation );
				writeVector( t.Scale );
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
					case GameObject ent:
						bn.Write( (byte)4 );
						writeInt( ent.Id.GetHashCode() ); // todo: maybe store as a bigint instead
						break;
					case Vector4 v:
						bn.Write( (byte)5 );
						writeVector4( v );
						break;
					case Color c:
						bn.Write( (byte)9 );
						writeVector4( c );
						break;
					case bool b:
						bn.Write( (byte)6 );
						bn.Write( b );
						break;
					case int i:
						bn.Write( (byte)7 );
						writeInt( i );
						break;
					case float f:
						bn.Write( (byte)8 );
						writeFloat( f );
						break;
					default:
						throw new Exception( "Invalid userdata " + o.GetType() );
				}
			}

			void writeEntity( DuplicatorData.DuplicatorItem ent )
			{
				bn.Write( ent.index );
				writeString( ent.packageId );
				writeString( ent.model );
				writeVector( ent.position );
				writeRotation( ent.rotation );
				writeVector( ent.scale );
				writeVector4( ent.color );
				bn.Write( ent.mass );
				bn.Write( ent.frozen );

				bn.Write( ent.components.Count );
				foreach ( var o in ent.components )
				{
					writeString( o.TypeName );
					bn.Write( o.Data.Count );
					foreach ( var kv in o.Data )
					{
						writeString( kv.Key );
						writeObject( kv.Value );
					}
				}
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
				switch ( constr.type )
				{
					case ConstraintType.Spring:
						bn.Write( constr.maxLength );
						bn.Write( constr.minLength );
						writeVector( constr.springLinear );
						break;
					case ConstraintType.Rope:
						bn.Write( constr.maxLength );
						bn.Write( constr.minLength );
						break;
					case ConstraintType.Axis:
						bn.Write( constr.minAngle );
						bn.Write( constr.maxAngle );
						bn.Write( constr.friction );
						break;
					case ConstraintType.BallSocket:
						bn.Write( constr.friction );
						break;
					case ConstraintType.Slider:
						bn.Write( constr.maxLength );
						bn.Write( constr.minLength );
						break;
				}
			}
		}

		private class DecoderV0
		{
			protected BinaryReader bn;
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
			protected Vector4 readVector4()
			{
				return new Vector4( bn.ReadSingle(), bn.ReadSingle(), bn.ReadSingle(), bn.ReadSingle() ); // Args eval left to right in C#
			}
			protected Rotation readRotation()
			{
				Rotation ret = new Rotation();
				ret.x = bn.ReadSingle(); ret.y = bn.ReadSingle(); ret.z = bn.ReadSingle(); ret.w = bn.ReadSingle();
				return ret;
			}
			protected virtual Transform readTransform()
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
					case 5:
						return readVector4();
					case 6:
						return bn.ReadBoolean();
					case 7:
						return bn.ReadInt32();
					case 8:
						return bn.ReadSingle();
					case 9:
						return (Color)readVector4();
					default:
						throw new Exception( "Invalid userdata type (" + type + ")" );
				}
			}
			protected virtual DuplicatorData.DuplicatorItem readEntity()
			{
				DuplicatorData.DuplicatorItem ret = new DuplicatorData.DuplicatorItem();
				ret.index = bn.ReadInt32();
				readString();
				ret.model = readString();
				ret.position = readVector();
				ret.rotation = readRotation();
				ret.frozen = bn.ReadBoolean();
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 1024 ); i < end; ++i )
				{
					readObject();
				}
				return ret;
			}
			protected virtual DuplicatorData.DuplicatorConstraint readConstraint()
			{
				DuplicatorData.DuplicatorConstraint ret = new DuplicatorData.DuplicatorConstraint();
				ret.type = (ConstraintType)bn.ReadByte();
				ret.entIndex1 = bn.ReadInt32();
				ret.entIndex2 = bn.ReadInt32();
				ret.bone1 = bn.ReadInt32();
				ret.bone2 = bn.ReadInt32();
				ret.anchor1 = readTransform();
				ret.anchor2 = readTransform();
				ret.collisions = bn.ReadBoolean();
				bn.ReadBoolean(); // ret.enableAngularConstraint
				bn.ReadBoolean(); // ret.enableLinearConstraint
				switch ( ret.type )
				{
					case ConstraintType.Spring:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						ret.springLinear = (PhysicsSpring)readVector();
						break;
					case ConstraintType.Axis:
						ret.minAngle = bn.ReadSingle();
						ret.maxAngle = bn.ReadSingle();
						break;
					case ConstraintType.Slider:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						break;
				}
				return ret;
			}
		}

		private class DecoderV1 : DecoderV0
		{
			public DecoderV1( BinaryReader bn_ ) : base( bn_ ) { }
			protected override Transform readTransform()
			{
				return new Transform( readVector(), readRotation(), readVector() );
			}

			protected override DuplicatorData.DuplicatorItem readEntity()
			{
				DuplicatorData.DuplicatorItem ret = new DuplicatorData.DuplicatorItem();
				ret.index = bn.ReadInt32();
				ret.packageId = readString();
				ret.model = readString();
				ret.position = readVector();
				ret.rotation = readRotation();
				ret.scale = readVector();
				ret.color = readVector4();
				ret.mass = bn.ReadSingle();
				ret.frozen = bn.ReadBoolean();
				for ( int i = 0, end = Math.Min( bn.ReadInt32(), 1024 ); i < end; ++i )
				{
					DuplicatorData.DuplicatorComponent comp = new DuplicatorData.DuplicatorComponent();
					comp.TypeName = readString();
					for ( int j = 0, jend = Math.Min( bn.ReadInt32(), 1024 ); j < jend; ++j )
					{
						comp.Data[readString()] = readObject();
					}
					ret.components.Add( comp );
				}
				return ret;
			}

			protected override DuplicatorData.DuplicatorConstraint readConstraint()
			{
				DuplicatorData.DuplicatorConstraint ret = new DuplicatorData.DuplicatorConstraint();
				ret.type = (ConstraintType)bn.ReadByte();
				ret.entIndex1 = bn.ReadInt32();
				ret.entIndex2 = bn.ReadInt32();
				ret.bone1 = bn.ReadInt32();
				ret.bone2 = bn.ReadInt32();
				ret.anchor1 = readTransform();
				ret.anchor2 = readTransform();
				ret.collisions = bn.ReadBoolean();
				switch ( ret.type )
				{
					case ConstraintType.Spring:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						ret.springLinear = (PhysicsSpring)readVector();
						break;
					case ConstraintType.Rope:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						break;
					case ConstraintType.Axis:
						ret.minAngle = bn.ReadSingle();
						ret.maxAngle = bn.ReadSingle();
						ret.friction = bn.ReadSingle();
						break;
					case ConstraintType.BallSocket:
						ret.friction = bn.ReadSingle();
						break;
					case ConstraintType.Slider:
						ret.maxLength = bn.ReadSingle();
						ret.minLength = bn.ReadSingle();
						ret.friction = bn.ReadSingle();
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
			return JsonSerializer.Deserialize<DuplicatorData>( data, new JsonSerializerOptions { IncludeFields = true } );
		}
	}

	public class DuplicatorData
	{
		private static HashSet<string> GenericIgnoredComponents = new() {
			"Prop",
			"ModelRenderer",
			"ModelCollider",
			"MapCollider",
			"Rigidbody",
			"PropHelper",
			"SkinnedModelRenderer",
			"ModelPhysics",
		};
		private static HashSet<string> GenericIgnoredComponentProperties = new()
		{
			"FogMode", // type FogInfluence
		};

		public class DuplicatorItem
		{
			public int index;
			public string packageId;
			public string model = "";
			public Vector3 position;
			public Rotation rotation;
			public Vector3 scale = Vector3.One;
			public Vector4 color = Color.White;
			public float mass;
			public bool frozen;
			public List<DuplicatorComponent> components = new();
			public DuplicatorItem() { }
			public DuplicatorItem( GameObject ent, Transform origin )
			{
				index = ent.Id.GetHashCode();
				if ( ent.GetComponent<ModelRenderer>() is ModelRenderer p )
				{
					model = p.Model.Name;
					packageId = SandboxGameManager.ModelToPackage.GetValueOrDefault( model, "" );
				}
				position = origin.PointToLocal( ent.WorldPosition );
				rotation = origin.RotationToLocal( ent.WorldRotation );
				scale = ent.WorldScale;
				var renderer = ent.GetComponent<ModelRenderer>();
				if ( renderer.IsValid() )
				{
					color = renderer.Tint;
				}
				var rigidBody = ent.GetComponent<Rigidbody>();
				mass = rigidBody?.MassOverride ?? 0;
				frozen = rigidBody?.PhysicsBody?.BodyType == PhysicsBodyType.Static;

				foreach ( var comp in ent.GetComponents<Component>() )
				{
					if ( GenericIgnoredComponents.Contains( comp.GetType().Name ) || comp is Joint ) continue;
					if ( comp is IDuplicatable duplicatableComp )
					{
						components.Add( new()
						{
							TypeName = duplicatableComp.GetType().Name,
							Data = duplicatableComp.PreDuplicatorCopy(),
						} );
					}
					else
					{
						// Log.Info( "Duplicator: genericComponent serializing " + model + "'s " + comp.GetType().Name );
						DuplicatorComponent savedComp = new()
						{
							TypeName = comp.GetType().Name,
						};
						TypeLibrary.GetSerializedObject( comp )
							.Where( property => property.HasAttribute<PropertyAttribute>() && property.IsPublic && !GenericIgnoredComponentProperties.Contains( property.Name ) ).ToList()
							.ForEach( property =>
							{
								var value = property.GetValue<object>();
								// Log.Info( "  " + property.Name + " = " + value );
								savedComp.Data[property.Name] = value;
							} );
						components.Add( savedComp );
					}
				}
			}

			internal static bool IsGameObjectAllowed( GameObject ent )
			{
				return ent.GetComponent<IDuplicatable>() is not null || ent.GetComponent<PropHelper>() is not null;
			}

			public GameObject Spawn( Transform origin )
			{
				var go = new GameObject()
				{
					WorldPosition = origin.PointToWorld( position ),
					WorldRotation = origin.RotationToWorld( rotation ),
					WorldScale = scale,
					Tags = { "solid" },
				};
				var prop = go.AddComponent<Prop>();
				prop.Model = Model.Load( model );

				var propHelper = go.GetOrAddComponent<PropHelper>();
				var renderer = go.GetComponent<ModelRenderer>();
				if ( renderer.IsValid() )
				{
					renderer.Tint = color;
				}
				var rigidBody = go.GetComponent<Rigidbody>();
				rigidBody.PhysicsBody.BodyType = PhysicsBodyType.Static;

				foreach ( var savedComp in components )
				{
					var typeInfo = TypeLibrary.GetType( savedComp.TypeName );
					Component newComp = go.Components.Create( typeInfo );
					if ( !newComp.IsValid() ) continue;

					if ( newComp is IDuplicatable duplicatableComp )
					{
						duplicatableComp.PostDuplicatorPaste( savedComp.Data );
					}
					else
					{
						foreach ( var (name, data) in savedComp.Data )
						{
							typeInfo.SetValue( newComp, name, data );
						}
					}
				}
				go.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
				go.Network.SetOrphanedMode( NetworkOrphaned.Host );
				go.NetworkSpawn();
				return go;
			}
		}

		public class DuplicatorComponent
		{
			public string TypeName;
			public Dictionary<string, object> Data = new();
		}
		public class DuplicatorConstraint
		{
			public ConstraintType type;
			public int entIndex1;
			public int entIndex2;
			public int bone1 = -1;
			public int bone2 = -1;
			public Transform anchor1;
			public Transform anchor2;
			public bool collisions;

			// spring
			public float maxLength;
			public float minLength;
			public PhysicsSpring springLinear;
			// hinge
			public float minAngle;
			public float maxAngle;
			public float friction;

			internal static bool IsJointAllowed( Joint joint )
			{
				return joint.Body1.IsValid() && joint.Body2.IsValid();
			}
			public DuplicatorConstraint() { }
			public DuplicatorConstraint( Joint joint )
			{
				anchor1 = joint.Point1.LocalTransform;
				anchor2 = joint.Point2.LocalTransform;
				entIndex1 = joint.Body1.GetGameObject().Id.GetHashCode();
				entIndex2 = joint.Body2.GetGameObject().Id.GetHashCode();
				// todo: bones, ragdolls
				collisions = joint.EnableCollision;
				type = joint.GetConstraintType();
				if ( type == ConstraintType.Weld ) { }
				else if ( type == ConstraintType.Spring && joint is Sandbox.SpringJoint springJoint )
				{
					maxLength = springJoint.MaxLength;
					minLength = springJoint.MinLength;
					springLinear = new PhysicsSpring( springJoint.Frequency, springJoint.Damping );
				}
				else if ( type == ConstraintType.Rope && joint is Sandbox.SpringJoint ropeJoint )
				{
					maxLength = ropeJoint.MaxLength;
					minLength = ropeJoint.MinLength;
				}
				else if ( type == ConstraintType.Axis && joint is Sandbox.HingeJoint hingeJoint )
				{
					minAngle = hingeJoint.MinAngle;
					maxAngle = hingeJoint.MaxAngle;
					friction = hingeJoint.Friction;
				}
				else if ( type == ConstraintType.BallSocket && joint is Sandbox.BallJoint ballJoint )
				{
					friction = ballJoint.Friction;
				}
				else if ( type == ConstraintType.Slider && joint is Sandbox.SliderJoint sliderJoint )
				{
					maxLength = sliderJoint.MaxLength;
					minLength = sliderJoint.MinLength;
					friction = sliderJoint.Friction;
				}
				else
				{
					Log.Warning( $"Duplicator: Unknown joint type {joint.GetType()}" );
					return;
				}
			}

			public void Spawn( Dictionary<int, GameObject> spawnedEnts )
			{
				var ent1 = spawnedEnts[entIndex1];
				var ent2 = spawnedEnts[entIndex2];
				if ( !ent1.IsValid() || !ent2.IsValid() )
				{
					Log.Warning( $"Duplicator: Failed to spawn {type}, missing entities" );
					return;
				}
				var anchor1World = ent1.Transform.World.ToWorld( anchor1 );
				var anchor2World = ent2.Transform.World.ToWorld( anchor2 );

				var propHelper1 = ent1.GetComponent<PropHelper>();

				Joint joint;
				if ( type == ConstraintType.Weld )
				{
					joint = propHelper1.Weld( ent2, !collisions, bone1, bone2 );
				}
				else if ( type == ConstraintType.Nocollide )
				{
					joint = propHelper1.NoCollide( ent2, bone1, bone2 );
				}
				else if ( type == ConstraintType.Spring )
				{
					joint = propHelper1.Spring( ent2, anchor1World.Position, anchor2World.Position, !collisions, bone1, bone2, minLength, maxLength, springLinear.Frequency, springLinear.Damping );
				}
				else if ( type == ConstraintType.Rope )
				{
					joint = propHelper1.Rope( ent2, anchor1World.Position, anchor2World.Position, !collisions, bone1, bone2, minLength, maxLength );
				}
				else if ( type == ConstraintType.Axis )
				{
					joint = propHelper1.Axis( ent2, anchor1World, !collisions, bone1, bone2, friction, minAngle, maxAngle );
				}
				else if ( type == ConstraintType.BallSocket )
				{
					joint = propHelper1.BallSocket( ent2, anchor1World, !collisions, bone1, bone2, friction );
				}
				else if ( type == ConstraintType.Slider )
				{
					joint = propHelper1.Slider( ent2, anchor1World.Position, anchor2World.Position, !collisions, bone1, bone2, minLength, maxLength, friction );
				}
				else
				{
					Log.Warning( $"Duplicator: Unknown joint type {type}" );
					return;
				}
				// Event.Run( "joint.spawned", joint, (Player)null );
			}
		}

		public string version = "1";
		public string name = "";
		public string author = "";
		public string date = "";
		public List<DuplicatorItem> entities = new List<DuplicatorItem>();
		public List<DuplicatorConstraint> joints = new List<DuplicatorConstraint>();
		public void Add( GameObject ent, Transform origin )
		{
			if ( DuplicatorItem.IsGameObjectAllowed( ent ) )
				entities.Add( new DuplicatorItem( ent, origin ) );
		}
		public void Add( Joint joint )
		{
			if ( DuplicatorConstraint.IsJointAllowed( joint ) )
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

	public class DuplicatorPasteJob : Component
	{
		Player owner;
		DuplicatorData data;
		Transform origin;
		Stopwatch timeUsed = new Stopwatch();
		Stopwatch timeElapsed = new Stopwatch();
		public Dictionary<int, GameObject> entList = new();
		Dictionary<int, DuplicatorData.DuplicatorItem> entData = new();
		public DuplicatorPasteJob Init( Player owner_, DuplicatorData data_, Transform origin_ )
		{
			owner = owner_;
			data = data_;
			origin = origin_;
			timeElapsed.Start();
			foreach ( var entData in data.entities )
			{
				if ( entData.packageId != "" )
					_ = SandboxGameManager.BroadcastMount( entData.packageId );
			}
			return this;
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
					Log.Warning( e, "Failed to spawn model (" + item.model + ")" );
				}
				if ( spawnedEnts == data.entities.Count )
				{
					foreach ( var (index, ent) in entList )
					{
						foreach ( var comp in ent.GetComponents<IDuplicatable>() )
							comp.PostDuplicatorPasteEntities( entList, entData[index].components.Find( compData => compData.TypeName == comp.GetType().Name )?.Data );
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
					var ent = entList[index];
					foreach ( var comp in ent.GetComponents<IDuplicatable>() )
						comp.PostDuplicatorPasteDone();

					if ( ent.GetComponent<Rigidbody>() is Rigidbody rigidBody && rigidBody.PhysicsBody.IsValid() )
					{
						if ( !entData[index].frozen )
						{
							// Enable physics after all entities are spawned, except for saved-as-frozen ents
							rigidBody.PhysicsBody.BodyType = PhysicsBodyType.Dynamic;
						}
					}
				}

				return false;
			}
		}

		protected override void OnFixedUpdate()
		{
			timeUsed.Start();
			while ( checkTime() )
			{
				if ( !next() )
				{
					DuplicatorTool.Pasting.Remove( owner );
					this.GameObject.Destroy();
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

namespace SandboxPlus.Tools
{
	[Library( "tool_duplicator", Title = "Duplicator", Description =
@"Save and load contraptions
Primary: Paste contraption
Secondary: Copy contraption (shift for area copy)", Group = "construction" )]
	public partial class DuplicatorTool : BaseTool
	{
		// Default behavior will be restoring the freeze state of entities to what they were when copied
		[ConVar( "tool_duplicator_unfreeze_all", ConVarFlags.Saved | ConVarFlags.UserInfo, Help = "Unfreeze all entities after pasting" )]
		public static bool UnfreezeAllAfterPaste { get; set; } = false;
		[ConVar( "tool_duplicator_area_size", ConVarFlags.Saved | ConVarFlags.UserInfo, Help = "Area copy size" )]
		public static float AreaSize { get; set; } = 250;

		public static Dictionary<Player, DuplicatorPasteJob> Pasting = new();
		public static Dictionary<Player, DuplicatorData> Selections = new(); // not networked yet, so probably will be 1:1 with the player, but this lets Selected last after the tool is disabled
		DuplicatorData Selected
		{
			get
			{
				if ( Selections.TryGetValue( Owner, out var sel ) )
					return sel;
				return null;
			}
			set
			{
				Selections[Owner] = value;
			}
		}
		float PasteRotationOffset { get; set; } = 0;
		float PasteHeightOffset { get; set; } = 0;
		bool AreaCopy { get; set; } = false;

		static new DuplicatorTool Instance
		{
			get
			{
				var tool = BaseTool.Instance;
				if ( tool is not DuplicatorTool dupe ) return null;
				return dupe;
			}
		}

		protected List<PreviewModel> previewModels = new();

		public override void Activate()
		{
			base.Activate();
			if ( !IsProxy )
			{
				if ( Selected != null )
				{
					DisplayGhosts( Selected.getGhosts() );
				}
			}
		}

		void DisplayGhosts( List<DuplicatorGhost> ghosts )
		{
			var tr = Parent.BasicTraceTool();
			DeletePreviews();
			foreach ( var ghost in ghosts )
			{
				PreviewModel previewModel = new PreviewModel
				{
					ModelPath = ghost.model,
					PositionOffset = ghost.position,
					RotationOffset = ghost.rotation,
					FaceNormal = false,
				};
				previewModel.Update( tr );
				previewModels.Add( previewModel );
			}
		}

		private void DeletePreviews()
		{
			foreach ( var preview in previewModels )
			{
				preview?.Destroy();
			}
			previewModels.Clear();
		}

		public override void Disabled()
		{
			base.Disabled();
			DeletePreviews();
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			DeletePreviews();
		}

		[ConCmd( "tool_duplicator_openfile", Help = "Loads a duplicator file" )]
		public static void OpenFile( string path )
		{
			if ( Instance is null )
			{
				ConsoleSystem.Run( "tool_current tool_duplicator" );
			}
			Instance.ReceiveDuplicatorData( FileSystem.OrganizationData.ReadAllBytes( "dupes/" + path ).ToArray() );
			Analytics.Increment( "duplicator.load" );
		}

		[ConCmd( "tool_duplicator_savefile", Help = "Saves a duplicator file" )]
		public static void SaveFile( string path )
		{
			if ( Instance is null )
			{
				ConsoleSystem.Run( "tool_current tool_duplicator" );
			}
			Instance.SaveDuplicatorData( path );
			Analytics.Increment( "duplicator.save" );
		}

		static void SaveFileData( string path, byte[] data )
		{
			FileSystem.OrganizationData.CreateDirectory( "dupes" );
			using ( Stream s = FileSystem.OrganizationData.OpenWrite( "dupes/" + path ) )
			{
				s.Write( data, 0, data.Length );
			}
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
				ResetTool();
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
				SaveFileData( path, data );
			}
			catch ( Exception e )
			{
				Log.Warning( $"Failed to save duplicator file: {e}" );
			}
		}

		void Copy( SceneTraceResult tr )
		{
			DuplicatorData copied = new()
			{
				author = Owner.Network.Owner.Name,
				date = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:sszz" )
			};

			var floorTr = tr.GameObject.IsWorld()
				? tr
				: Scene.Trace.Ray( tr.EndPosition, tr.EndPosition + new Vector3( 0, 0, -1e6f ) )
					.IgnoreDynamic() // only hit world
					.Run();

			Transform origin = new Transform( floorTr.Hit ? floorTr.EndPosition : tr.EndPosition );
			PasteRotationOffset = 0;
			PasteHeightOffset = 0;

			HashSet<GameObject> objs = new();
			HashSet<Joint> joints = new();
			if ( AreaCopy )
			{
				AreaCopy = false;
				var areaSize = new Vector3( int.Parse( GetConvarValue( "tool_duplicator_area_size", "250" ) ) );
				foreach ( var ent in Scene.FindInPhysics( new BBox( origin.Position - areaSize, origin.Position + areaSize ) ) )
				{
					if ( DuplicatorData.DuplicatorItem.IsGameObjectAllowed( ent ) )
						ent.GetAttachedGameObjects( joints, objs );
				}
			}
			else if ( tr.GameObject.IsValid() )
			{
				tr.GameObject.GetAttachedGameObjects( joints, objs );
			}
			foreach ( var ob in objs )
				copied.Add( ob, origin );
			foreach ( var j in joints )
				copied.Add( j );
			Log.Info( "Duplicator: Copied " + copied.entities.Count + " entities and " + copied.joints.Count + " joints" );

			DisplayGhosts( copied.getGhosts() );
			Selected = copied.entities.Count > 0 ? copied : null;
			Analytics.Increment( "duplicator.copy" );
		}

		void Paste( SceneTraceResult tr )
		{
			// We can add rotation back in once the ghosts also rotate
			// var modelRotation = Rotation.From( new Angles( 0, Owner.EyeRotation.Angles().yaw, 0 ) );
			var goPaste = new GameObject()
			{
				WorldPosition = tr.EndPosition,
				WorldRotation = Rotation.From( new Angles( 0, PasteRotationOffset, 0 ) ),
				NetworkMode = NetworkMode.Never,
				Tags = { "duplicatorPasteJob" }
			};
			Pasting[Owner] = goPaste.AddComponent<DuplicatorPasteJob>().Init( Owner, Selected, new Transform( tr.EndPosition + new Vector3( 0, 0, PasteHeightOffset ) ) );
			var dupeName = Selected.name;
			var entList = Pasting[Owner].entList;
			UndoSystem.Add( creator: Owner, () =>
			{
				foreach ( var ent in entList.Values )
				{
					ent?.Destroy();
				}
				if ( goPaste.IsValid() && Pasting[Owner] == goPaste.GetComponent<DuplicatorPasteJob>() )
				{
					goPaste.Destroy();
					Pasting.Remove( Owner );
				}
				return $"Removed duplicator paste {dupeName}";
			} );
			Analytics.Increment( "duplicator.paste" );
		}

		public override bool Primary( SceneTraceResult trace )
		{
			if ( !Input.Pressed( "attack1" ) ) return false; ;
			if ( Pasting.ContainsKey( Owner ) ) return false;

			if ( trace.Hit && Selected is not null )
			{
				Paste( trace );
				return true;
			}
			return false;
		}

		public override bool Secondary( SceneTraceResult trace )
		{
			if ( !Input.Pressed( "attack2" ) ) return false;
			if ( Pasting.ContainsKey( Owner ) ) return false;

			AreaCopy = Input.Down( "run" );

			if ( trace.Hit && trace.GameObject.IsValid() )
			{
				Copy( trace );
				return true;
			}
			return false;
		}

		protected override void OnUpdate()
		{
			if ( Input.Down( "use" ) )
			{
				PasteRotationOffset += Input.MouseDelta.x;
				Input.MouseDelta = new Vector3();
			}
			if ( Input.Pressed( "SlotNext" ) && Input.Down( "use" ) )
			{
				PasteHeightOffset += 5;
			}
			if ( Input.Pressed( "SlotPrev" ) && Input.Down( "use" ) )
			{
				PasteHeightOffset -= 5;
			}

			if ( previewModels.Count > 0 )
			{
				var trace = Parent.BasicTraceTool();
				if ( trace.Hit )
				{
					foreach ( var preview in previewModels )
					{
						preview.Update( trace );
					}
				}
				else
				{
					DeletePreviews();
				}
			}
		}

		public void ResetTool()
		{
			Selected = null;
			DisplayGhosts( new List<DuplicatorGhost>() );
		}
	}
}
