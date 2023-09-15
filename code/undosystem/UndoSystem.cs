using System;
using Sandbox;


namespace UndoManager
{
	public partial class UndoManager
	{
		[Event( "entity.spawned" )]
		public static void OnSpawned( Entity spawned, Entity owner )
		{
			if ( owner is Player player )
			{
				Undoer.Add( player.GetActiveController().Client, spawned );
			}
		}

		[Event( "undo.add" )]
		public static void OnAddUndo( Func<string> undoable, Entity owner )
		{
			if ( owner is Player player )
			{
				Undoer.Add( player.GetActiveController().Client, undoable );
			}
		}

		[GameEvent.Client.BuildInput]
		public static void ProcessClientInput()
		{
			if ( Input.Pressed( "undo" ) )
			{
				if ( Input.Down( "run" ) )
				{
					OnRedo();
				}
				else
				{
					OnUndo();
				}
			}
		}

		[ConCmd.Server( "undo" )]
		public static async void OnUndo()
		{
			var client = ConsoleSystem.Caller;

			if ( client == null ) return;

			foreach ( Undo undo in Undoer.Get( client.SteamId ).ToArray() )
			{
				var creator = undo.Creator;
				var prop = undo.Prop;
				var time = undo.Time;

				if ( undo.Avoid ) continue;
				if ( undo.Undoable != null )
				{
					var undoMessage = undo.Undoable();
					Undoer.DoUndo( creator, null, undo );
					if ( undoMessage != "" )
					{
						HintFeed.AddHint( To.Single( creator ), "undo", undoMessage );
						CreateUndoParticles( To.Single( creator ), Vector3.Zero );
						break;
					}
					else
					{
						continue; // this one wasn't needed, try another
					}
				}
				if ( !prop.IsValid() )
				{
					Undoer.DoUndo( creator, prop, undo );

					continue;
				}

				CreateUndoParticles( To.Single( creator ), prop.Position );

				// Display undo msg for props and some non props
				var msg = GetPropName( prop );
				HintFeed.AddHint( To.Single( creator ), "undo", msg );

				if ( prop.GetType() != typeof( Prop ) )
				{
					Undoer.DoUndo( creator, prop, undo );
					break;
				}

				var redo = Redoer.Add( creator, prop, undo );

				undo.Avoid = true;

				Undoer.HideProp( undo );

				await prop.Task.DelaySeconds( Redoer.Duration );
				if ( undo.Time + Redoer.Duration < Time.Now )
					Undoer.DoUndo( creator, prop, undo, redo );

				break;
			}
		}

		[ConCmd.Server( "redo" )]
		public static void OnRedo()
		{
			var client = ConsoleSystem.Caller;

			if ( client == null ) return;

			foreach ( Redo redo in Redoer.Get( client.SteamId ) )
			{
				var creator = redo.Creator;
				var prop = redo.Prop;
				var undo = redo.Undo;

				if ( !prop.IsValid() )
				{
					Undoer.DoUndo( creator, prop, undo, redo );

					continue;
				}

				CreateRedoParticles( To.Single( creator ), prop.Position );

				Redoer.OnRedone( To.Single( creator ) );
				Redoer.ResetProp( redo );
				Redoer.Remove( creator, redo );

				var msg = GetPropName( prop );
				HintFeed.AddHint( To.Single( creator ), "redo", msg );

				undo.Avoid = false;
				undo.Time = Time.Now;

				break;
			}
		}

		[ClientRpc]
		public static void CreateUndoParticles( Vector3 pos )
		{
			using ( Prediction.Off() )
			{
				if ( pos != Vector3.Zero )
				{
					Particles.Create( "particles/physgun_freeze.vpcf", pos + Vector3.Up * 2 );
				}
				Game.LocalPawn?.PlaySound( "drop_001" );
			}
		}

		[ClientRpc]
		public static void CreateRedoParticles( Vector3 pos )
		{
			using ( Prediction.Off() )
			{
				Game.LocalPawn?.PlaySound( "drop_002" );
			}
		}

		/// <summary>
		/// Try to get a human readable name for an entity/prop.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static string GetPropName( Entity e )
		{
			var displayInfo = DisplayInfo.For( e );
			var msg = displayInfo.Name;

			// We can do better!
			if ( e.GetType() == typeof( Prop ) )
			{
				if ( e is ModelEntity model )
				{
					msg += $" ({model.Model?.ResourceName})";
				}
			}

			return msg;
		}
	}
}
