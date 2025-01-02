namespace SandboxPlus;

public sealed partial class PlayerController : Component
{
	/// <summary>
	/// Create a ragdoll gameobject version of our render body.
	/// </summary>
	public GameObject CreateRagdoll( string name = "Ragdoll" )
	{
		var go = new GameObject( true, name );
		go.Tags.Add( "ragdoll" );
		go.WorldTransform = WorldTransform;

		var originalBody = Renderer.Components.Get<SkinnedModelRenderer>();

		if ( !originalBody.IsValid() )
			return go;

		var mainBody = go.Components.Create<SkinnedModelRenderer>();
		mainBody.CopyFrom( originalBody );
		mainBody.UseAnimGraph = false;

		// copy the clothes
		foreach ( var clothing in originalBody.GameObject.Children.SelectMany( x => x.Components.GetAll<SkinnedModelRenderer>() ) )
		{
			if ( !clothing.IsValid() ) continue;

			var newClothing = new GameObject( true, clothing.GameObject.Name );
			newClothing.Parent = go;

			var item = newClothing.Components.Create<SkinnedModelRenderer>();
			item.CopyFrom( clothing );
			item.BoneMergeTarget = mainBody;
		}

		var physics = go.Components.Create<ModelPhysics>();
		physics.Model = mainBody.Model;
		physics.Renderer = mainBody;
		physics.CopyBonesFrom( originalBody, true );


		return go;
	}
}
