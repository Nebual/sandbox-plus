using Sandbox;
using Sandbox.UI;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stacker
{
	public enum StackDirections
	{
		Front,
		Back,
		Right,
		Left,
		Up,
		Down,
	};

	public enum StackRelatativeTo
	{
		Prop,
		//World,
	};

	public struct StackPropData
	{
		public Vector3 Position { get; set; }
		public Rotation Rotation { get; set; }
	}

	public struct AxisAngles
	{
		public Vector3 Pitch { get; set; }
		public Vector3 Yaw { get; set; }
		public Vector3 Roll { get; set; }

		public AxisAngles(Vector3 pitch, Vector3 yaw, Vector3 roll)
		{
			Pitch = pitch;
			Yaw = yaw;
			Roll = roll;
		}
	}

	[Library( "tool_stacker", Title = "Stacker", Description = "Stack multiple of a prop in a direction", Group = "construction" )]
	public class StackerTool : BaseTool
	{
		protected StackerPreviewModels previewModels;

		[ConVar( "tool_stacker_amount" )] private static float _ { get; set; } = 3f;
		[ConVar( "tool_stacker_relative_to" )] private static StackRelatativeTo _1 { get; set; } = StackRelatativeTo.Prop;
		[ConVar( "tool_stacker_direction" )] private static StackDirections _2 { get; set; } = StackDirections.Front;
		[ConVar( "tool_stacker_x_offset" )] private static float _3 { get; set; } = 0f;
		[ConVar( "tool_stacker_y_offset" )] private static float _4 { get; set; } = 0f;
		[ConVar( "tool_stacker_z_offset" )] private static float _5 { get; set; } = 0f;
		[ConVar( "tool_stacker_pitch_offset" )] private static float _6 { get; set; } = 0f;
		[ConVar( "tool_stacker_yaw_offset" )] private static float _7 { get; set; } = 0f;
		[ConVar( "tool_stacker_roll_offset" )] private static float _8 { get; set; } = 0f;
		[ConVar( "tool_stacker_ghost_all" )] private static bool _11 { get; set; } = true;
		[ConVar( "tool_stacker_draw_xyz" )] private static bool _12 { get; set; } = true;
		[ConVar( "tool_stacker_draw_xyz_labels" )] private static bool _13 { get; set; } = true;

		public override bool Primary( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
				return false;

			var player = Player.FindLocalPlayer();
			if ( !player.IsValid() )
				return false;

			if ( Input.Pressed( "attack1" ) )
			{
				if ( IsTraceValid( trace ) )
				{
					GameObject traceGO = trace.GameObject;
					List<StackPropData> propData = GeneratePropData( traceGO );

					// Stop ghosting, we're about to spawn
					previewModels.SetEnabled( false );

					List<GameObject> created = new();

					foreach (StackPropData prop in propData)
					{
						GameObject newGO = traceGO.Clone(prop.Position, prop.Rotation);

						Rigidbody rigidbody = newGO.Components.Get<Rigidbody>();
						if ( rigidbody.IsValid() && rigidbody.PhysicsBody.IsValid() )
							rigidbody.PhysicsBody.BodyType = PhysicsBodyType.Static;

						newGO.NetworkSpawn( player.GameObject.Network.Owner );
						newGO.Network.SetOrphanedMode( NetworkOrphaned.Host );

						created.Add( newGO );
					}

					UndoSystem.Add( player, () =>
					{
						foreach(GameObject obj in created)
						{
							obj.DestroyAsync( 0 );
						}
						return "Undone stacking prop " + created.Count + " times";
					} );

					return true;
				}
				return false;
			}

			return false;
		}

		public override bool Secondary( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
				return false;

			if ( Input.Pressed( "attack2" ) )
			{
				return true;
			}

			return false;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var trace = Parent.BasicTraceTool();

			bool DrawXYZ = bool.Parse( GetConvarValue( "tool_stacker_draw_xyz" ) );
			bool DrawXYZLabels = bool.Parse( GetConvarValue( "tool_stacker_draw_xyz_labels" ) );

			if ( DrawXYZ ) DoAxisOverlay( trace, Scene.Camera, DrawLabels: DrawXYZLabels );

			if ( previewModels != null )
			{
				if ( IsTraceValid( trace ) )
				{
					List<StackPropData> propData = GeneratePropData( trace.GameObject );

					previewModels.Update( trace, propData );
				}
				else
				{
					previewModels.SetEnabled( false );
				}
			}
		}

		private List<StackPropData> GeneratePropData( GameObject target )
		{
			int tool_stacker_amount = int.Parse( GetConvarValue( "tool_stacker_amount" ) );

			string relativeTo = GetConvarValue( "tool_stacker_relative_to" );
			string stackDirection = GetConvarValue( "tool_stacker_direction" );

			float XOffset = float.Parse( GetConvarValue( "tool_stacker_x_offset" ) );
			float YOffset = float.Parse( GetConvarValue( "tool_stacker_y_offset" ) );
			float ZOffset = float.Parse( GetConvarValue( "tool_stacker_z_offset" ) );

			float PitchOffset = float.Parse( GetConvarValue( "tool_stacker_pitch_offset" ) );
			float YawOffset = float.Parse( GetConvarValue( "tool_stacker_yaw_offset" ) );
			float RollOffset = float.Parse( GetConvarValue( "tool_stacker_roll_offset" ) );

			bool GhostAll = bool.Parse( GetConvarValue( "tool_stacker_ghost_all" ) );

			Prop propComponent = target.GetComponent<Prop>();

			Vector3 size = propComponent.Model.Bounds.Size;

			Vector3 currentLoc = propComponent.WorldPosition;
			Rotation currentRot = target.WorldRotation;

			Vector3 userOffset = new Vector3( XOffset, YOffset, ZOffset );
			Angles userRotation = new Angles( PitchOffset, YawOffset, RollOffset );

			Vector3 distance = GetDistance( stackDirection, size );


			List<StackPropData> propData = new();
			for ( var i = 0; i < (GhostAll ? tool_stacker_amount : 1); i++ )
			{

				StackPropData prop = new StackPropData();

				Vector3 direction = GetDirection( relativeTo, stackDirection, currentRot );
				Vector3 offsetUse = GetOffset( stackDirection, currentRot, userOffset );

				Vector3 newLoc = currentLoc + (direction * distance) + offsetUse;

				// TODO: Help!
				Rotation newRot = RotateAngle( stackDirection , currentRot, userRotation );

				prop.Position = newLoc;
				prop.Rotation = newRot;

				currentLoc = newLoc;
				currentRot = newRot;

				propData.Add( prop );
			}

			return propData;
		}

		/// <summary>
		/// Calculates the direction to point the entity to by depending on whether the stack is
		/// created relative to the world or the original prop, and the direction to stack in.
		/// </summary>
		/// <param name="relativeTo">A string from the StackRelatativeTo enum, denoting what we're </param>
		/// <param name="direction">A string direction from the StackDirections enum</param>
		/// <param name="rot">Rotation</param>
		/// <returns>Vector3 denoting the direction we're stacking in</returns>
		private Vector3 GetDirection( string relativeTo, string direction, Rotation rot )
		{
			if ( relativeTo == StackRelatativeTo.Prop.ToString() )
			{
				if ( direction == StackDirections.Front.ToString() ) return rot.Forward;
				else if ( direction == StackDirections.Back.ToString() ) return rot.Backward;
				else if ( direction == StackDirections.Right.ToString() ) return rot.Right;
				else if ( direction == StackDirections.Left.ToString() ) return rot.Left;
				else if ( direction == StackDirections.Up.ToString() ) return rot.Up;
				else if ( direction == StackDirections.Down.ToString() ) return rot.Down;
			}
			else
			{
				if ( direction == StackDirections.Front.ToString() ) return Vector3.Forward;
				else if ( direction == StackDirections.Back.ToString() ) return Vector3.Backward;
				else if ( direction == StackDirections.Right.ToString() ) return Vector3.Right;
				else if ( direction == StackDirections.Left.ToString() ) return Vector3.Left;
				else if ( direction == StackDirections.Up.ToString() ) return Vector3.Up;
				else if ( direction == StackDirections.Down.ToString() ) return Vector3.Down;
			}

			return Vector3.Zero;
		}

		/// <summary>
		/// Calculates the space occupied by the entity depending on the stack direction.
		/// This represents the number of units to offset the stack entities so they appear
		/// directly in front of the previous entity( depending on direction).
		/// </summary>
		/// <param name="direction">A string direction from the StackDirections enum</param>
		/// <param  name="bboxSize">A Vector3 containing the model (or bbox) size of the entity we're stacking</param>
		/// <returns></returns>
		private float GetDistance( string direction, Vector3 bboxSize )
		{
			if ( direction == StackDirections.Front.ToString() ) return bboxSize.x;
			else if ( direction == StackDirections.Back.ToString() ) return bboxSize.x;
			else if ( direction == StackDirections.Right.ToString() ) return bboxSize.y;
			else if ( direction == StackDirections.Left.ToString() ) return bboxSize.y;
			else if ( direction == StackDirections.Up.ToString() ) return bboxSize.z;
			else if ( direction == StackDirections.Down.ToString() ) return bboxSize.y;

			return 0;
		}

		/// <summary>
		/// Calculates a direction vector used for offsetting a stacked entity based on the facing angle of the previous entity.
		/// </summary>
		/// <param name="direction">A string direction from the StackDirections enum</param>
		/// <param name="rot">The Rotation value we're currently using</param>
		/// <param name="offset">The Vector3 of the currently configured offset parameters</param>
		/// <returns></returns>
		private Vector3 GetOffset( string direction, Rotation rot, Vector3 offset )
		{
			if ( direction == StackDirections.Front.ToString() ) 
				return (rot.Forward * offset.x) + (rot.Up * offset.z) + (rot.Right * offset.y);
			else if ( direction == StackDirections.Back.ToString() )
				return (rot.Backward * offset.x) + (rot.Up * offset.z) + (rot.Left * offset.y);
			else if ( direction == StackDirections.Right.ToString() )
				return (rot.Right * offset.x) + (rot.Up * offset.z) + (rot.Backward * offset.y);
			else if ( direction == StackDirections.Left.ToString() )
				return (rot.Left * offset.x) + (rot.Up * offset.z) + (rot.Forward * offset.y);
			else if ( direction == StackDirections.Up.ToString() )
				return (rot.Up * offset.x) + (rot.Backward * offset.z) + (rot.Right * offset.y);
			else if ( direction == StackDirections.Down.ToString() )
				return (rot.Down * offset.x) + (rot.Forward * offset.z) + (rot.Right * offset.y);

			return Vector3.Zero;
		}

		/// <summary>
		/// Rotates the first angle by the second angle. This ensures proper rotation
		/// along all three axes and prevents various problems related to simply adding
		/// two angles together.
		/// </summary>
		/// <param name="direction">A string direction from the StackDirections enum</param>
		/// <param name="angle">The current Rotation that we're looking to adjust</param>
		/// <param name="rotation">The Angles we're adjusting by</param>
		/// <returns></returns>
		private Rotation RotateAngle(string direction, Rotation angle, Angles rotation)
		{
			var axisAngles = GetRotation( direction, angle );

			angle.RotateAroundAxis( axisAngles.Pitch.Normal, rotation.pitch );
			angle.RotateAroundAxis( axisAngles.Yaw.Normal, rotation.yaw );
			angle.RotateAroundAxis( axisAngles.Roll.Normal, rotation.roll );

			return angle;
		}

		private AxisAngles GetRotation(string direction, Rotation angle)
		{
			if ( direction == StackDirections.Front.ToString() )
				return new AxisAngles( angle.Right, angle.Up, angle.Forward);
			else if ( direction == StackDirections.Back.ToString() )
				return new AxisAngles( angle.Left, angle.Up, angle.Right );
			else if ( direction == StackDirections.Right.ToString() )
				return new AxisAngles( angle.Right, angle.Up, angle.Backward );
			else if ( direction == StackDirections.Left.ToString() )
				return new AxisAngles( angle.Left, angle.Up, angle.Forward );
			else if ( direction == StackDirections.Up.ToString() )
				return new AxisAngles( angle.Up, angle.Backward, angle.Right );
			else if ( direction == StackDirections.Down.ToString() )
				return new AxisAngles( angle.Down, angle.Forward, angle.Right );

			return new AxisAngles();
		}

		protected override bool IsPreviewTraceValid( SceneTraceResult tr )
		{
			return false;
		}

		private bool IsTraceValid( SceneTraceResult trace )
		{
			Prop prop = trace.GameObject.GetComponent<Prop>();

			if ( !trace.Hit || !trace.Body.IsValid() || !prop.IsValid() )
				return false;

			return true;
		}

		public override bool Reload( SceneTraceResult trace )
		{
			if ( !trace.Hit || !trace.Body.IsValid() || !trace.GameObject.GetComponent<Prop>().IsValid() )
				return false;

			return false;
		}

		public override void CreateToolPanel()
		{
			var toolConfigUi = new StackerToolConfig();
			SpawnMenu.Instance?.ToolPanel?.AddChild( toolConfigUi );
		}

		public override void Activate()
		{
			base.Activate();

			if ( previewModels != null )
			{
				previewModels.Destroy();
			}
			CreatePreview();
		}

		public override void CreatePreview()
		{
			previewModels = new StackerPreviewModels
			{
				Stacker = this
			};
		}

		public override void Disabled()
		{
			previewModels?.Destroy();
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			previewModels?.Destroy();
		}
	}

}

