namespace Sandbox.Tools;

[Library( "tool_light", Title = "Light", Description = "A dynamic point light", Group = "construction" )]
public partial class LightTool : LampTool
{
	[Property, Title( "Model" ), ModelProperty( SpawnLists = ["light"] )]
	public override string SpawnModel { get; set; } = "models/light/light_tubular.vmdl";

	protected override TypeDescription GetSpawnedComponent()
	{
		return TypeLibrary.GetType<LightComponent>();
	}
	protected override void UpdateEntity( GameObject go )
	{
		var lamp = go.GetComponent<LightComponent>();
		lamp.Enabled = true;
		lamp.Shadows = true;
		lamp.Radius = 512;
		lamp.Attenuation = 1f;
		go.GetComponent<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.Off; // otherwise the light itself casts shadows from the inside

		go.GetOrAddComponent<LightWireComponent>();
	}
}
