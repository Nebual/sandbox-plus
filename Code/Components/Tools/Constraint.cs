using System.Collections.Generic;
using System.Linq;
using System;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox.Physics;

namespace Sandbox.Tools
{
	[Library( "tool_constraint", Title = "Constraint", Description = "Constrain stuff together", Group = "construction" )]
	public partial class ConstraintTool : BaseTool
	{
		// ConVar doesn't seem to network its wrapped property nicely, so lets make our own...
		[ConVar( "tool_constraint_type" )]
		public static ConstraintType _ { get; set; } = ConstraintType.Weld;
		public ConstraintType Type
		{
			get
			{
				var _ = Enum.TryParse( GetConvarValue( "tool_constraint_type" ), out ConstraintType val );
				return val;
			}
			private set
			{
				ConsoleSystem.Run( "tool_constraint_type", value.ToString() );
			}
		}

		[ConVar( "tool_constraint_nudge_distance" )] public static string _2 { get; set; } = "10";
		[ConVar( "tool_constraint_nudge_percent" )] public static string _3 { get; set; } = "0";
		[ConVar( "tool_constraint_move_target" )] public static string _4 { get; set; } = "1";
		[ConVar( "tool_constraint_move_offset" )] public static string _5 { get; set; } = "0";
		[ConVar( "tool_constraint_move_percent" )] public static string _6 { get; set; } = "0";
		[ConVar( "tool_constraint_rotate_target" )] public static string _7 { get; set; } = "1";
		[ConVar( "tool_constraint_rotate_snap" )] public static string _8 { get; set; } = "15";
		[ConVar( "tool_constraint_freeze_target" )] public static string _9 { get; set; } = "1";
		[ConVar( "tool_constraint_nocollide_target" )] public static string _10 { get; set; } = "1";
		[ConVar( "tool_constraint_rope_extra_length" )] public static string _11 { get; set; } = "0";
		[ConVar( "tool_constraint_rope_rigid" )] public static string _12 { get; set; } = "0";

		private enum ConstraintToolStage
		{
			Waiting,
			Moving,
			Rotating,
			Applying,
			ConstraintController,
			Removing,
		}

		private ConstraintToolStage stage { get; set; } = ConstraintToolStage.Waiting;
		private SceneTraceResult trace1;
		private SceneTraceResult trace2;
		private PhysicsJoint createdJoint;
		private Func<string> createdUndo;
		private bool wasMoved;
		private bool wasFrozen;
		private bool wasSleeping;
		private bool wasMotionEnabled;
		private float rotationBuildUp;
		private bool wasPlayerInputLocked = false;
		private const float RotateSpeed = 30.0f;

		// Dynamic entrypoint for optional Wirebox support, if installed
		public static Action<Player, SceneTraceResult, ConstraintType, PhysicsJoint, Func<string>> CreateWireboxConstraintController;
		private static bool WireboxSupport
		{
			get => CreateWireboxConstraintController != null;
		}

		protected override void OnUpdate()
		{
			if ( stage == ConstraintToolStage.Rotating )
			{
				// Lock view angles while we're using the mouse to rotate a prop
				if ( Owner.IsValid() && Owner.Controller.IsValid() )
				{
					Owner.Controller.UseInputControls = false;
					Owner.Controller.WishVelocity = 0;
					wasPlayerInputLocked = true;
				}
			}
			else if ( wasPlayerInputLocked )
			{
				if ( Owner.IsValid() && Owner.Controller.IsValid() )
				{
					Owner.Controller.UseInputControls = true;
				}
				wasPlayerInputLocked = false;
			}

			this.Description = CalculateDescription();

			if ( Input.Pressed( "drop" ) )
			{
				SelectNextType();
			}


			if ( stage == ConstraintToolStage.Rotating )
			{
				if ( !trace1.Body.IsValid() || !trace2.Body.IsValid() )
				{
					Reset();
				}

				var rotationAmount = Input.MouseDelta.x * RotateSpeed * Time.Delta;
				var rotationSnap = float.Parse( GetConvarValue( "tool_constraint_rotate_snap", "0" ) );
				if ( rotationSnap >= 0.001 )
				{
					// todo: snap rotation relative to the base prop (or World angles I guess), rather than the target prop
					rotationBuildUp += rotationAmount;

					if ( rotationBuildUp <= -rotationSnap )
					{
						rotationBuildUp = 0;
						var rotation = Rotation.FromAxis( trace2.Normal, -rotationSnap );
						trace1.GameObject.WorldTransform = trace1.GameObject.WorldTransform.RotateAround( trace2.HitPosition, rotation );
					}
					else if ( rotationBuildUp >= rotationSnap )
					{
						rotationBuildUp = 0;
						var rotation = Rotation.FromAxis( trace2.Normal, rotationSnap );
						trace1.GameObject.WorldTransform = trace1.GameObject.WorldTransform.RotateAround( trace2.HitPosition, rotation );
					}
				}
				else
				{
					var rotation = Rotation.FromAxis( trace2.Normal, rotationAmount );
					trace1.GameObject.WorldTransform = trace1.GameObject.WorldTransform.RotateAround( trace2.HitPosition, rotation );
				}
			}
		}

		public override bool Primary( SceneTraceResult trace )
		{
			if ( !Input.Pressed( "attack1" ) ) return false;

			if ( stage == ConstraintToolStage.Waiting )
			{
				trace1 = trace;
				stage = ConstraintToolStage.Moving;
				return true;
			}

			if ( stage == ConstraintToolStage.Moving )
			{
				trace2 = trace;
				if ( !trace1.Body.IsValid() )
				{
					Reset();
					return false;
				}

				if ( trace1.GameObject.IsWorld() && trace2.GameObject.IsWorld() )
				{
					return false; // can't both be world
				}

				if ( GetConvarValue( "tool_constraint_move_target" ) != "0" && Type != ConstraintType.Nocollide )
				{
					var wantsRotation = GetConvarValue( "tool_constraint_rotate_target" ) != "0" && !trace1.GameObject.IsWorld();

					wasFrozen = (trace1.Body.BodyType == PhysicsBodyType.Static);
					wasMotionEnabled = trace1.Body.MotionEnabled;
					wasSleeping = trace1.Body.Sleeping;

					if ( wantsRotation )
					{
						trace1.Body.Sleeping = false;
						trace1.Body.BodyType = PhysicsBodyType.Keyframed;
						trace1.Body.MotionEnabled = false;
					}

					var offset = float.Parse( GetConvarValue( "tool_constraint_move_offset" ) );
					if ( GetConvarValue( "tool_constraint_move_percent" ) != "0" )
					{
						offset = GetEntityOffsetPercent( offset, trace1 );
					}

					trace1.GameObject.WorldTransform = CalculateNewTransform(
						trace1.GameObject.WorldTransform,
						trace1.Normal,
						trace1.EndPosition,
						trace2.Normal,
						trace2.EndPosition,
						offset
					);
					wasMoved = true;

					if ( wantsRotation )
					{
						stage = ConstraintToolStage.Rotating;
						rotationBuildUp = 0;
						return true;
					}
				}

				// Don't return here because we can skip straight to applying
				stage = ConstraintToolStage.Applying;
			}

			if ( stage == ConstraintToolStage.Rotating )
			{
				// Don't return here because we can skip straight to applying
				stage = ConstraintToolStage.Applying;
			}

			if ( stage == ConstraintToolStage.Applying )
			{
				return ApplyConstraint();
			}
			else if ( stage == ConstraintToolStage.ConstraintController )
			{
				// only reachable if Wirebox's installed
				if ( WireboxSupport )
				{
					CreateWireboxConstraintController( Owner, trace, Type, createdJoint, createdUndo );
				}
				Reset();
				return true;
			}
			return false;
		}

		protected bool ApplyConstraint()
		{
			if ( !trace1.GameObject.IsWorld() )
			{
				if ( GetConvarValue( "tool_constraint_freeze_target" ) != "0" )
				{
					trace1.Body.Sleeping = true;
					trace1.Body.MotionEnabled = wasMotionEnabled;
					trace1.Body.BodyType = PhysicsBodyType.Static;
				}
				else
				{
					trace1.Body.MotionEnabled = wasMotionEnabled;
					trace1.Body.Sleeping = wasSleeping;
					trace1.Body.BodyType = wasFrozen ? PhysicsBodyType.Static : PhysicsBodyType.Dynamic;
				}
			}

			if ( Type == ConstraintType.Weld )
			{
				ApplyWeld();
			}
			else if ( Type == ConstraintType.Nocollide )
			{
				ApplyNocollide();
			}
			else if ( Type == ConstraintType.Spring )
			{
				ApplySpring();
			}
			else if ( Type == ConstraintType.Rope )
			{
				ApplyRope();
			}
			else if ( Type == ConstraintType.Axis )
			{
				ApplyAxis();
			}
			else if ( Type == ConstraintType.BallSocket )
			{
				ApplyBallsocket();
			}
			else if ( Type == ConstraintType.Slider )
			{
				ApplySlider();
			}
			if ( GetConvarValue( "tool_constraint_freeze_target" ) == "0" && !trace1.GameObject.IsWorld() )
			{
				trace1.Body.Sleeping = false;
			}
			return true;
		}

		protected void ApplyWeld()
		{
			var point1 = PhysicsPoint.World( trace1.Body, trace2.EndPosition, trace2.Body.Rotation );
			var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Body.Rotation );
			var joint = PhysicsJoint.CreateFixed(
				point1,
				point2
			);
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplyNocollide()
		{
			var point1 = PhysicsPoint.World( trace1.Body, trace1.EndPosition, trace2.Body.Rotation );
			var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Body.Rotation );
			// this is kinda weird for a nocollide, ideally we'd have a dedicated constraint or maybe a modified FixedJoint
			var joint = PhysicsJoint.CreateLength(
				point1,
				point2,
				9999999
			);
			joint.Collisions = false;

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplySpring()
		{
			var position1 = wasMoved ? trace2.EndPosition : trace1.EndPosition;
			var point1 = PhysicsPoint.World( trace1.Body, position1, trace2.Body.Rotation );
			var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Body.Rotation );
			var length = position1.Distance( trace2.EndPosition );
			var joint = PhysicsJoint.CreateSpring(
				point1,
				point2,
				length,
				length
			);
			joint.SpringLinear = new( 5.0f, 0.7f );
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			var rope = MakeRope( position1, trace1, trace2 );

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				rope?.Destroy();
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplyRope()
		{
			var position1 = wasMoved ? trace2.EndPosition : trace1.EndPosition;
			var point1 = PhysicsPoint.World( trace1.Body, position1, trace2.Body.Rotation );
			var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Body.Rotation );
			var lengthOffset = float.Parse( GetConvarValue( "tool_constraint_rope_extra_length" ) );
			var length = position1.Distance( trace2.EndPosition ) + lengthOffset;
			var joint = PhysicsJoint.CreateLength(
				point1,
				point2,
				length
			);
			joint.SpringLinear = new( 1000.0f, 0.7f );
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			if ( GetConvarValue( "tool_constraint_rope_rigid" ) == "1" )
			{
				joint.MinLength = length;
			}

			var rope = MakeRope( position1, trace1, trace2 );

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				rope?.Destroy();
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplyAxis()
		{
			var position1 = wasMoved ? trace2.EndPosition : trace1.EndPosition;
			var pivot = Input.Down( "run" )
				? trace1.Body.MassCenter
				: position1;

			var pivotTransform = new Transform( pivot, Rotation.LookAt( Rotation.LookAt( trace1.Normal ).Up ) );
			var joint = PhysicsJoint.CreateHinge(
				trace1.Body,
				trace2.Body,
				trace1.Body.Transform.ToLocal( pivotTransform ),
				trace2.Body.Transform.ToLocal( pivotTransform )
			);
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplyBallsocket()
		{
			var position1 = wasMoved ? trace2.EndPosition : trace1.EndPosition;
			var pivot = Input.Down( "run" )
				? trace1.Body.MassCenter
				: position1;

			var joint = PhysicsJoint.CreateBallSocket(
				PhysicsPoint.World( trace1.Body, pivot, trace1.Body.Rotation ),
				PhysicsPoint.World( trace2.Body, pivot, trace2.Body.Rotation )
			);
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		protected void ApplySlider()
		{
			var position1 = wasMoved ? trace2.EndPosition : trace1.EndPosition;
			var point1 = PhysicsPoint.World( trace1.Body, position1, trace2.Body.Rotation );
			var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, trace2.Body.Rotation );
			var joint = PhysicsJoint.CreateSlider(
				point1,
				point2,
				0,
				0 // can be used like a rope hybrid, to limit max length
			);
			joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";

			var rope = MakeRope( position1, trace1, trace2 );
			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			trace2.GameObject.GetComponent<PropHelper>().PhysicsJoints.Add( joint );
			FinishConstraintCreation( joint, () =>
			{
				rope?.Destroy();
				if ( joint.IsValid() )
				{
					joint.Remove();
					return $"Removed {Type} constraint";
				}
				return "";
			} );
		}

		public override bool Secondary( SceneTraceResult tr )
		{
			if ( !Input.Pressed( "attack2" ) ) return false;

			return Nudge( tr, Input.Down( "run" ) ? 1 : -1 );
		}

		public override bool Reload( SceneTraceResult trace )
		{
			if ( !Input.Pressed( "reload" ) ) return false;

			if ( !trace.Body.IsValid() || trace.GameObject.IsWorld() )
			{
				return false;
			}
			if ( stage == ConstraintToolStage.Waiting )
			{
				trace1 = trace;
				stage = ConstraintToolStage.Removing;
				if ( Input.Down( "walk" ) )
				{
					RemoveConstraints( Type, trace );
					Reset();
					return true;
				}
			}
			else if ( stage == ConstraintToolStage.Removing )
			{
				trace2 = trace;
				if ( !trace1.Body.IsValid() )
				{
					Reset();
					return false;
				}
				RemoveConstraintBetweenEnts( Type, trace1, trace2 );
				Reset();
				return true;
			}

			return false;
		}

		private bool Nudge( SceneTraceResult tr, int direction )
		{
			if ( !tr.Body.IsValid() || tr.GameObject.IsWorld() )
			{
				return false;
			}
			var offset = float.Parse( GetConvarValue( "tool_constraint_nudge_distance" ) );
			if ( GetConvarValue( "tool_constraint_nudge_percent" ) != "0" )
			{
				offset = GetEntityOffsetPercent( offset, tr );
			}
			tr.GameObject.WorldPosition += tr.Normal * offset * direction;
			tr.Body.Sleeping = true;
			return true;
		}

		private float GetEntityOffsetPercent( float percent, SceneTraceResult tr )
		{
			if ( Math.Abs( tr.Normal.Dot( tr.GameObject.WorldRotation.Forward ) ) > 0.8f )
			{
				return tr.GameObject.GetBounds().Size.x * percent / 100f;
			}
			else if ( Math.Abs( tr.Normal.Dot( tr.GameObject.WorldRotation.Left ) ) > 0.8f )
			{
				return tr.GameObject.GetBounds().Size.y * percent / 100f;
			}
			else
			{
				return tr.GameObject.GetBounds().Size.z * percent / 100f;
			}
		}

		private void RemoveConstraints( ConstraintType type, SceneTraceResult tr )
		{
			tr.GameObject.GetComponent<PropHelper>().PhysicsJoints.ForEach( j =>
			{
				if ( j.GetConstraintType() == type )
				{
					j.Remove();
				}
			} );
		}

		private void RemoveConstraintBetweenEnts( ConstraintType type, SceneTraceResult trace1, SceneTraceResult trace2 )
		{
			trace1.GameObject.GetComponent<PropHelper>().PhysicsJoints.ForEach( j =>
			{
				if ( (j.Body1 == trace1.Body || j.Body2 == trace1.Body) && (j.Body1 == trace2.Body || j.Body2 == trace2.Body) )
				{
					if ( j.GetConstraintType() == type )
					{
						j.Remove();
					}
				}
			} );
		}

		private void SelectNextType()
		{
			IEnumerable<ConstraintType> possibleEnums = Enum.GetValues<ConstraintType>();
			if ( Input.Down( "run" ) )
			{
				possibleEnums = possibleEnums.Reverse();
			}
			Type = possibleEnums.SkipWhile( e => e != Type ).Skip( 1 ).FirstOrDefault();
		}

		private string CalculateDescription()
		{
			var desc = $"Constraint entities together using {Type} constraint";
			if ( Type == ConstraintType.Axis )
			{
				if ( stage == ConstraintToolStage.Waiting )
				{
					desc += $"\nFirst, {Input.GetButtonOrigin( "attack1" )} the part that spins (eg. wheel).";
				}
				else if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nSecond, {Input.GetButtonOrigin( "attack1" )} the base. Hold {Input.GetButtonOrigin( "run" )} to use wheel's center of mass.";
				}
			}
			else
			{
				if ( stage == ConstraintToolStage.Waiting )
				{
					desc += $"\nFirst, {Input.GetButtonOrigin( "attack1" )} the part to attach.";
				}
				else if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nSecond, {Input.GetButtonOrigin( "attack1" )} the base.";
				}
			}
			if ( stage == ConstraintToolStage.Waiting )
			{
				desc += $"\n{Input.GetButtonOrigin( "attack2" )} to nudge ({Input.GetButtonOrigin( "run" )} for reverse)";
				desc += $"\n{Input.GetButtonOrigin( "reload" )} to select an entity to remove {Type} constraint ({Input.GetButtonOrigin( "walk" )} to remove all {Type} constraints)";
				desc += $"\n{Input.GetButtonOrigin( "drop" )} to cycle to next constraint type";
			}
			if ( WireboxSupport )
			{
				if ( stage == ConstraintToolStage.Moving )
				{
					desc += $"\nHold {Input.GetButtonOrigin( "walk" )} to begin creating a Wire Constraint Controller";
				}
				else if ( stage == ConstraintToolStage.ConstraintController )
				{
					desc += $"\nFinally, place the Wire Constraint Controller";
				}
			}
			return desc;
		}

		private void FinishConstraintCreation( PhysicsJoint joint, Func<string> undo )
		{
			joint.OnBreak += () => { undo(); };

			// Event.Run( "joint.spawned", joint, Owner );
			UndoSystem.Add( Owner, undo );
			// Analytics.ServerIncrement( To.Single( Owner ), "constraint.created" );

			if ( WireboxSupport && Input.Down( "walk" ) )
			{
				createdJoint = joint;
				createdUndo = undo;
				stage = ConstraintToolStage.ConstraintController;
				return;
			}
			Reset();
		}

		private static LegacyParticleSystem MakeRope( Vector3 position1, SceneTraceResult trace1, SceneTraceResult trace2 )
		{
			var rope = Particles.MakeParticleSystem( "particles/entity/rope.vpcf", trace1.GameObject.WorldTransform, 0, trace1.GameObject );
			var RopePoints = new List<ParticleControlPoint>();

			if ( trace1.GameObject.IsWorld() )
			{
				RopePoints.Add( new() { StringCP = "0", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = position1 } );
			}
			else
			{
				var p = new GameObject();
				p.SetParent( trace1.GameObject );
				p.LocalPosition = trace1.Body.Transform.PointToLocal( position1 );

				RopePoints.Add( new() { StringCP = "0", Value = ParticleControlPoint.ControlPointValueInput.GameObject, GameObjectValue = p } );
			}
			if ( trace2.GameObject.IsWorld() )
			{
				RopePoints.Add( new() { StringCP = "1", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = trace2.EndPosition } );
			}
			else
			{
				var p = new GameObject();
				p.SetParent( trace2.GameObject );
				p.LocalPosition = trace2.Body.Transform.PointToLocal( trace2.EndPosition );

				RopePoints.Add( new() { StringCP = "1", Value = ParticleControlPoint.ControlPointValueInput.GameObject, GameObjectValue = p } );
			}
			rope.ControlPoints = RopePoints;

			return rope;
		}

		public override void CreateToolPanel()
		{
			var toolConfigUi = new ConstraintToolConfig();
			SpawnMenu.Instance?.ToolPanel?.AddChild( toolConfigUi );
		}

		private void Reset()
		{
			stage = ConstraintToolStage.Waiting;
			wasMoved = false;
		}

		public override void Activate()
		{
			base.Activate();

			Reset();
		}

		public override void Disabled()
		{
			base.Disabled();

			Reset();
		}
		/*
		PreviewEntity previewModel;

		protected override bool IsPreviewTraceValid( SceneTraceResult tr )
		{
			if ( stage != ConstraintToolStage.Moving || !trace1.GameObject.IsValid() )
				return false;

			if ( !base.IsPreviewTraceValid( tr ) )
				return false;

			return true;
		}

		public override void CreatePreviews()
		{
			if ( !Game.IsClient )
			{
				return;
			}
			TryCreatePreview( ref previewModel, "models/citizen_props/crate01.vmdl" );
		}

		public override void UpdatePreviews()
		{
			if ( !Game.IsClient )
			{
				return;
			}
			CreatePreviews();
			base.UpdatePreviews();

			var tr2 = DoTrace();
			if ( !IsPreviewTraceValid( tr2 ) )
			{
				return;
			}
			if ( previewModel.IsValid() )
			{
				previewModel.Model = trace1.GameObject.GetComponent<Prop>().Model;
				previewModel.Transform = CalculateNewTransform( trace1.GameObject.Transform, trace1.Normal, trace1.EndPosition, tr2.Normal, tr2.EndPosition, float.Parse( GetConvarValue( "tool_constraint_move_offset" ) ) );
			}
		}
		*/

		private static Transform CalculateNewTransform( Transform originalTransform, Vector3 normal1, Vector3 endPos1, Vector3 normal2, Vector3 endPos2, float offset = 0f )
		{
			// Calculate the new rotation
			var transform = originalTransform;
			var rotation = transform.Rotation;
			var axis1Rotation = transform.RotationToLocal( Rotation.LookAt( normal1 ) );
			var axis2Rotation = transform.RotationToLocal( Rotation.LookAt( -normal2 ) );
			var rotationDifference = Rotation.Difference( axis1Rotation, axis2Rotation );
			var newRotation = rotation * rotationDifference;

			// The position offset has to be calculated before we apply the rotation
			var localOffset = transform.PointToLocal( endPos1 + normal1 * offset );
			transform.Rotation = newRotation;
			var newPosition = endPos2 - transform.PointToWorld( localOffset );
			transform.Position += newPosition;

			return transform;
		}
	}

	public enum ConstraintType
	{
		Weld,
		Nocollide, // Generic
		Axis, // Revolute
		BallSocket, // Spherical
		Rope,
		Spring, // Winch/Hydraulic
		Slider, // Prismatic
	}
}
