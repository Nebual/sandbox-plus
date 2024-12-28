using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_material", Title = "Material", Description = "Primary: Set Material Override\nSecondary: Change Model's MaterialGroup (if supported)\nReload: Clear Material Override", Group = "construction" )]
	public partial class MaterialTool : BaseTool
	{
		[ConVar( "tool_material_material" )]
		public static string _ { get; set; } = "";

		[ConVar( "tool_material_materialindex" )] public static string _2 { get; set; } = "-1";
		protected override void OnUpdate()
		{
			base.OnUpdate();
			if ( IsProxy ) return;

			var tr = Parent.BasicTraceTool();
			if ( tr.GameObject.IsValid() )
			{
				var modelEnt = tr.GameObject.GetComponent<ModelRenderer>();
				if ( !modelEnt.IsValid() )
					return;

				if ( Input.Pressed( "attack1" ) )
				{
					modelEnt.SetClientMaterialOverride( GetConvarValue( "tool_material_material" ), int.Parse( GetConvarValue( "tool_material_materialindex" ) ) );

					Parent.ToolEffects( tr.EndPosition, tr.Normal );
				}
				else if ( Input.Pressed( "attack2" ) )
				{
					if ( modelEnt.Model.MaterialGroupCount < 2 )
					{
						return;
					}
					var materialIndex = modelEnt.Model.GetMaterialGroupIndex( modelEnt.MaterialGroup );
					if ( materialIndex == -1 )
					{
						// skip the first (default) one
						materialIndex = 1;
					}
					else if ( materialIndex < (modelEnt.Model.MaterialGroupCount - 1) )
					{
						materialIndex++;
					}
					else
					{
						// cycle back to start
						materialIndex = 0;
					}
					modelEnt.MaterialGroup = modelEnt.Model.GetMaterialGroupName( materialIndex );

					Parent.ToolEffects( tr.EndPosition, tr.Normal );
				}
				else if ( Input.Pressed( "reload" ) )
				{
					ConsoleSystem.Run( "tool_material_materialindex", "-1" ); // for now, until there's ui
					modelEnt.SetClientMaterialOverride( "" );

					Parent.ToolEffects( tr.EndPosition, tr.Normal );
				}
			}
		}

		[Rpc.Broadcast]
		public static void SetEntityMaterialOverride( ModelRenderer modelEnt, string material, int materialIndex = -1 )
		{
			GameTask.MainThread().OnCompleted( async () =>
			{
				if ( !modelEnt.IsValid() ) return;
				if ( material != "" && !material.EndsWith( ".vmat" ) )
				{
					var package = await Package.FetchAsync( material, false, true );
					if ( package == null )
					{
						Log.Warning( $"Material: Tried to load material package {material} - which was not found" );
						return;
					}
					if ( !package.IsMounted() )
					{
						HintFeed.AddHint( "", "Material not loaded, mounting...", 2 );
					}

					await package.MountAsync();
					material = package.GetCachedMeta( "SingleAssetSource", "" );
					if ( material == "" )
					{
						Log.Warning( $"Material2: package {material} lacks SingleAssetSource - is it actually a Material?" );
						return;
					}
				}
				if ( material == "" )
				{
					if ( materialIndex == -1 )
					{
						modelEnt?.SetMaterialOverride( null, "" );
						modelEnt.ClearMaterialOverrides();
					}
					else
					{
						modelEnt?.SetMaterialOverride( null, "materialIndex" + materialIndex );
					}
				}
				else
				{
					if ( materialIndex == -1 || modelEnt.Model == null )
					{
						modelEnt?.SetMaterialOverride( Material.Load( material ), "" );
					}
					else
					{
						var mats = modelEnt.Model.Materials.ToList();
						for ( int i = 0; i < mats.Count; i++ )
						{
							mats[i].Attributes.Set( "materialIndex" + i, 1 );
						}
						modelEnt?.SetMaterialOverride( Material.Load( material ), "materialIndex" + materialIndex );
					}
				}
			} );
		}

		public static async void SpawnlistsInitialize()
		{
			var collectionLookup = await Package.FetchAsync( "sugmatextures/sugmatextures", false, true );
			ModelSelector.AddToSpawnlist( "material", collectionLookup.PackageReferences );
		}

		public override void CreateToolPanel()
		{
			var materialSelector = new MaterialSelector();
			SpawnMenu.Instance?.ToolPanel?.AddChild( materialSelector );
		}
	}
}


public static partial class ModelEntityExtensions
{
	public static void SetClientMaterialOverride( this ModelRenderer instance, string material, int materialIndex = -1 )
	{
		Sandbox.Tools.MaterialTool.SetEntityMaterialOverride( instance, material, materialIndex );
	}
}
