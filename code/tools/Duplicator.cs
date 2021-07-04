using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sandbox
{
	public class Duplicatable
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
		public static List<Dictionary<string, object>> Decode( byte[] data )
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

		static byte[] Encode( List<Dictionary<string, object>> entityData )
		{
			using ( var stream = new MemoryStream() )
			using ( var bn = new BinaryWriter( stream ) )
			{
				bn.Write( (uint)0x44555045 ); // File type 'DUPE'
				bn.Write( (byte)0 ); // Encoder version
				return stream.GetBuffer();
			}
		}

		static List<Dictionary<string, object>> DecodeV0( BinaryReader reader )
		{
			return new List<Dictionary<string, object>>();
		}
	}
}

namespace Sandbox.Tools
{
	[Library( "tool_duplicator", Title = "Duplicator", Description = "Save and load contraptions", Group = "construction" )]
	public partial class DuplicatorTool : BaseTool
	{
		static HashSet<string> GenericClasses = new HashSet<string>();
		static void GenericFactory(string classname)
		{
			var ent = Library.Create<Prop>( classname );
			//ent.Position = ;
			//ent.Rotation = ;
			//ent.PhysicsEnabled = ;
			//ent.EnableSolidCollisions =;
			//ent.Massless = ;
		}

		// Default behavior will be restoring the freeze state of entities to what they were when copied
		[ConVar.ClientData( "tool_duplicator_freeze_all" )]
		public bool FreezeAllAfterPaste { get; set; } = false;
		[ConVar.ClientData( "tool_duplicator_unfreeze_all" )]
		public bool UnfreezeAllAfterPaste { get; set; } = false;


		List<PreviewEntity> ghosts = new List<PreviewEntity>();
		public override void CreatePreviews()
		{

		}


		void Copy( TraceResult tr )
		{

		}

		void Paste( TraceResult tr )
		{

		}

		void ToolFired(InputButton input)
		{
			var startPos = Owner.EyePos;
			var dir = Owner.EyeRot.Forward;
			var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance ).Ignore( Owner ).Run();

			switch(input)
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
					ToolFired( InputButton.Attack1 );
				if ( Input.Pressed( InputButton.Attack2 ) )
					ToolFired( InputButton.Attack2 );
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
