using Sandbox;
using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_material", Title = "Material", Description = "Primary: Set Material Override\nSecondary: Change Model's MaterialGroup (if supported)\nReload: Clear Material Override", Group = "construction" )]
	public partial class MaterialTool : BaseTool
	{
		[ConVar.ClientData( "tool_material_material" )]
		public static string _ { get; set; } = "";

		[ConVar.ClientData( "tool_material_materialindex" )] public static string _2 { get; set; } = "-1";
		public override void Simulate()
		{
			using ( Prediction.Off() )
			{

				var tr = DoTrace();

				if ( !tr.Hit || !tr.Entity.IsValid() )
					return;

				if ( tr.Entity is not ModelEntity modelEnt )
					return;

				if ( Input.Pressed( "attack1" ) )
				{
					modelEnt.SetClientMaterialOverride( GetConvarValue( "tool_material_material" ), int.Parse( GetConvarValue( "tool_material_materialindex" ) ) );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
				else if ( Input.Pressed( "attack2" ) )
				{
					if ( modelEnt.MaterialGroupCount == 0 )
					{
						return;
					}
					modelEnt.SetMaterialGroup( modelEnt.GetMaterialGroup() + 1 );
					if ( modelEnt.GetMaterialGroup() == 0 )
					{
						// cycle back to start
						modelEnt.SetMaterialGroup( 0 );
					}

					CreateHitEffects( tr.EndPosition, tr.Normal, true );
				}
				else if ( Input.Pressed( "reload" ) )
				{
					if ( Game.IsClient )
					{
						ConsoleSystem.Run( "tool_material_materialindex", "-1" ); // for now, until there's ui
					}
					modelEnt.SetClientMaterialOverride( "" );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
			}
		}

		[ClientRpc]
		public static async void SetEntityMaterialOverride( ModelEntity modelEnt, string material, int materialIndex = -1 )
		{
			if ( Game.IsClient )
			{
				if ( material != "" && !material.EndsWith( ".vmat" ) )
				{
					var package = await Package.FetchAsync( material, false, true );
					if ( package == null )
					{
						Log.Warning( $"Material: Tried to load material package {material} - which was not found" );
						return;
					}

					await package.MountAsync( false );
					material = package.GetCachedMeta( "SingleAssetSource", "" );
					if ( material == "" )
					{
						Log.Warning( $"Material2: package {material} lacks SingleAssetSource - is it actually a Material?" );
						return;
					}
				}
				// modelEnt.SetMaterialOverride does not seem to work until the prop is touched, yet SceneObject.SetMaterialOverride only works _until_ its touched, so set both
				if ( material == "" )
				{
					modelEnt?.ClearMaterialOverride();
					modelEnt?.SceneObject?.ClearMaterialOverride();
				}
				else
				{
					if ( materialIndex == -1 || modelEnt.Model == null )
					{
						modelEnt?.SetMaterialOverride( material );
						modelEnt?.SceneObject?.SetMaterialOverride( Material.Load( material ) );
					}
					else
					{
						var mats = modelEnt.Model.Materials.ToList();
						for ( int i = 0; i < mats.Count; i++ )
						{
							mats[i].Attributes.Set( "materialIndex" + i, 1 );
						}
						modelEnt?.SetMaterialOverride( Material.Load( material ), "materialIndex" + materialIndex );
						modelEnt?.SceneObject?.SetMaterialOverride( Material.Load( material ), "materialIndex" + materialIndex, 1 );
					}
				}
			}
		}

		[Event( "spawnlists.initialize" )]
		public static async void SpawnlistsInitialize()
		{
			var collectionLookup = await Package.FetchAsync( "sugmatextures/sugmatextures", false, true );
			ModelSelector.AddToSpawnlist( "material", collectionLookup.PackageReferences );
		}

		public override void CreateToolPanel()
		{
			if ( Game.IsClient )
			{
				var materialSelector = new MaterialSelector();
				SpawnMenu.Instance?.ToolPanel?.AddChild( materialSelector );
			}
		}
	}
}


public static partial class ModelEntityExtensions
{
	public static void SetClientMaterialOverride( this ModelEntity instance, string material, int materialIndex = -1 )
	{
		Sandbox.Tools.MaterialTool.SetEntityMaterialOverride( instance, material, materialIndex );
	}
}
