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
		// ConVar.ClientData doesn't seem to network its wrapped property nicely, so lets make our own...
		[ConVar.ClientData( "tool_constraint_type" )]
		public static ConstraintType _ { get; set; } = ConstraintType.Weld;
		private ConstraintType Type
		{
			get
			{
				var _ = Enum.TryParse( GetConvarValue( "tool_constraint_type" ), out ConstraintType val );
				return val;
			}
			set
			{
				ConsoleSystem.Run( "tool_constraint_type", value.ToString() );
			}
		}

		[ConVar.ClientData( "tool_constraint_nudge_distance" )] public static string _2 { get; set; } = "10";
		[ConVar.ClientData( "tool_constraint_nudge_percent" )] public static string _3 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_move_target" )] public static string _4 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_move_offset" )] public static string _5 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_move_percent" )] public static string _6 { get; set; } = "0";
		[ConVar.ClientData( "tool_constraint_rotate_target" )] public static string _7 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_rotate_snap" )] public static string _8 { get; set; } = "15";
		[ConVar.ClientData( "tool_constraint_freeze_target" )] public static string _9 { get; set; } = "1";
		[ConVar.ClientData( "tool_constraint_nocollide_target" )] public static string _10 { get; set; } = "1";

		[Net, Predicted]
		private int stage { get; set; } = 0;
		private TraceResult trace1;
		private TraceResult trace2;
		private PhysicsJoint createdJoint;
		private Func<string> createdUndo;


		// Dynamic entrypoint for optional Wirebox support, if installed
		public static Action<Player, TraceResult, ConstraintType, PhysicsJoint, Func<string>> CreateWireboxConstraintController;
		private static bool WireboxSupport
		{
			get => CreateWireboxConstraintController != null;
		}

		public override void Simulate()
		{
			if ( Game.IsClient )
			{
				this.Description = CalculateDescription();

				if ( Input.Pressed( "drop" ) )
				{
					SelectNextType();
				}
			}

			using ( Prediction.Off() )
			{

				if ( !Game.IsServer )
					return;

				var tr = DoTrace();

				if ( !tr.Hit || !tr.Entity.IsValid() )
				{
					return;
				}


				if ( Input.Pressed( "attack1" ) )
				{
					if ( stage == 0 )
					{
						trace1 = tr;
						stage++;
					}
					else if ( stage == 1 )
					{
						trace2 = tr;
						if ( !trace1.Entity.IsValid() )
						{
							Reset();
							return;
						}
						if ( trace1.Entity.IsWorld && trace2.Entity.IsWorld )
						{
							return; // can't both be world
						}
						var point1 = PhysicsPoint.World( trace1.Body, trace1.EndPosition, Rotation.LookAt( -trace1.Normal, trace1.Direction ) );
						var point2 = PhysicsPoint.World( trace2.Body, trace2.EndPosition, Rotation.LookAt( trace2.Normal, trace2.Direction ) );

						trace1.Body.Sleeping = true;
						if ( GetConvarValue( "tool_constraint_freeze_target" ) != "0" && !trace1.Entity.IsWorld )
						{
							trace1.Body.BodyType = PhysicsBodyType.Static;
						}

						if ( Type == ConstraintType.Weld )
						{
							var joint = PhysicsJoint.CreateFixed(
								point1,
								point2
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							trace1.Body.Sleeping = false;

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
						else if ( Type == ConstraintType.Nocollide )
						{
							var joint = PhysicsJoint.CreateFixed(
								point1,
								point2
							);
							joint.EnableAngularConstraint = false;
							joint.EnableLinearConstraint = false;
							joint.Collisions = false;
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
						else if ( Type == ConstraintType.Spring )
						{
							var length = trace1.EndPosition.Distance( trace2.EndPosition );
							var joint = PhysicsJoint.CreateSpring(
								point1,
								point2,
								length,
								length
							);
							joint.SpringLinear = new( 5.0f, 0.7f );
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							joint.EnableAngularConstraint = false;

							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Rope )
						{
							var joint = PhysicsJoint.CreateLength(
								point1,
								point2,
								trace1.EndPosition.Distance( trace2.EndPosition )
							);
							joint.SpringLinear = new( 1000.0f, 0.7f );
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							joint.EnableAngularConstraint = false;

							var rope = MakeRope( trace1, trace2 );

							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						else if ( Type == ConstraintType.Axis )
						{
							var pivot = Input.Down( "run" )
								? trace1.Body.MassCenter
								: trace1.EndPosition;

							var joint = PhysicsJoint.CreateHinge(
								trace1.Body,
								trace2.Body,
								pivot,
								trace1.Normal
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							trace1.Body.Sleeping = false;

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
						else if ( Type == ConstraintType.BallSocket )
						{
							var pivot = Input.Down( "run" )
								? trace1.Body.MassCenter
								: trace1.EndPosition;

							var joint = PhysicsJoint.CreateBallSocket(
								trace1.Body,
								trace2.Body,
								pivot
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							trace1.Body.Sleeping = false;

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
						else if ( Type == ConstraintType.Slider )
						{
							var joint = PhysicsJoint.CreateSlider(
								point1,
								point2,
								0,
								0 // can be used like a rope hybrid, to limit max length
							);
							joint.Collisions = GetConvarValue( "tool_constraint_nocollide_target" ) == "0";
							var rope = MakeRope( trace1, trace2 );
							FinishConstraintCreation( joint, () =>
							{
								rope?.Destroy( true );
								if ( joint.IsValid() )
								{
									joint.Remove();
									return $"Removed {Type} constraint";
								}
								return "";
							} );
						}
						if ( GetConvarValue( "tool_constraint_freeze_target" ) == "0" && !trace1.Entity.IsWorld )
						{
							trace1.Body.Sleeping = false;
						}
					}
					else if ( stage == 2 )
					{
						// only reachable if Wirebox's installed
						if ( WireboxSupport )
						{
							CreateWireboxConstraintController( Owner, tr, Type, createdJoint, createdUndo );
						}
						Reset();
					}
				}
				else if ( Input.Pressed( "attack2" ) )
				{
					Reset();
				}
				else if ( Input.Pressed( "reload" ) )
				{
					if ( tr.Entity is not Prop prop )
					{
						return;
					}

					// todo: how to remove all constraints from X, where are they stored?

					Reset();
				}
				else
				{
					return;
				}

				CreateHitEffects( tr.EndPosition, tr.Normal );
			}
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
			var desc = $"Constraint entities together using a {Type} constraint";
			if ( Type == ConstraintType.Axis )
			{
				if ( stage == 0 )
				{
					desc += $"\nFirst, shoot the part that spins (eg. wheel).";
				}
				else if ( stage == 1 )
				{
					desc += $"\nSecond, shoot the base. Hold shift to use wheel's center of mass.";
				}
			}
			else
			{
				if ( stage == 1 )
				{
					desc += $"\nSecond, shoot the base.";
				}
			}
			if ( WireboxSupport )
			{
				if ( stage == 1 )
				{
					desc += $"\nHold alt to begin creating a Wire Constraint Controller";
				}
				else if ( stage == 2 )
				{
					desc += $"\nFinally, place the Wire Constraint Controller";
				}
			}
			return desc;
		}

		private void FinishConstraintCreation( PhysicsJoint joint, Func<string> undo )
		{
			joint.OnBreak += () => { undo(); };

			Event.Run( "joint.spawned", joint, Owner );
			Event.Run( "undo.add", undo, Owner );

			if ( WireboxSupport && Input.Down( "walk" ) )
			{
				createdJoint = joint;
				createdUndo = undo;
				stage = 2;
				return;
			}
			Reset();
		}

		private static Particles MakeRope( TraceResult trace1, TraceResult trace2 )
		{
			var rope = Particles.Create( "particles/rope.vpcf" );

			if ( trace1.Body.GetEntity().IsWorld )
			{
				rope.SetPosition( 0, trace1.EndPosition );
			}
			else
			{
				rope.SetEntityBone( 0, trace1.Body.GetEntity(), trace1.Bone, new Transform( trace1.Body.GetEntity().Transform.PointToLocal( trace1.EndPosition ) ) );
			}
			if ( trace2.Body.GetEntity().IsWorld )
			{
				rope.SetPosition( 1, trace2.EndPosition );
			}
			else
			{
				rope.SetEntityBone( 1, trace2.Body.GetEntity(), trace2.Bone, new Transform( trace2.Body.GetEntity().Transform.PointToLocal( trace2.EndPosition ) ) );
			}
			return rope;
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				var toolConfigUi = new ConstraintToolConfig();
				SpawnMenu.Instance?.ToolPanel?.AddChild( toolConfigUi );
			}
		}

		private void Reset()
		{
			stage = 0;
		}

		public override void Activate()
		{
			base.Activate();

			Reset();

		}

		public override void Deactivate()
		{
			base.Deactivate();

			Reset();
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
				// Nudge, // not a constraint, but something this tool can independently do
	}
}
