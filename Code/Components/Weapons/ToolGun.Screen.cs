partial class ToolGun
{
	private Texture screenTexture;
	private ToolgunPanel screenPanel;
	private SceneCustomObject screenRenderObject;

	private void SetupToolgunPanel()
	{
		screenRenderObject = new SceneCustomObject( Scene.SceneWorld )
		{
			RenderOverride = ToolgunScreenRender
		};

		screenPanel = new()
		{
			RenderedManually = true,
			PanelBounds = new Rect( 0, 0, 1024, 1024 ),
		};

		UpdateToolgunPanel();

		screenTexture = Texture.CreateRenderTarget().WithSize( screenPanel.PanelBounds.Size ).Create();
	}
	private void DestroyToolgunPanel()
	{
		screenRenderObject?.Delete();
		screenRenderObject = null;

		screenPanel?.Delete();
		screenPanel = null;
		// screenTexture.Dispose();
	}
	private void UpdateToolgunPanel()
	{
		if ( !screenPanel.IsValid() ) return;

		var toolName = DisplayInfo.For( CurrentTool ).Name;
		screenPanel.CurrentToolName = toolName;
		if ( CurrentTool is SandboxPlus.Tools.ConstraintTool ctool )
		{
			screenPanel.CurrentToolName = $"{toolName} | {ctool.Type.ToString()}";
		}

		if ( WorldModel.SceneObject.IsValid() )
		{
			WorldModel.SceneObject.Batchable = false;
			WorldModel.SceneObject.Attributes.Set( "screenTexture", screenTexture );
		}

		if ( ViewModel?.Renderer?.SceneObject.IsValid() == true )
		{
			ViewModel.Renderer.SceneObject.Batchable = false;
			ViewModel.Renderer.SceneObject.Attributes.Set( "screenTexture", screenTexture );
		}
	}
	private void ToolgunScreenRender( SceneObject sceneObject )
	{
		if (screenTexture == null) return;
		Graphics.RenderTarget = RenderTarget.From( screenTexture );
		Graphics.Attributes.SetCombo( "D_WORLDPANEL", 0 );
		Graphics.Viewport = new Rect( 0, screenPanel.PanelBounds.Size );
		Graphics.Clear();

		screenPanel.RenderManual();

		Graphics.RenderTarget = null;
	}
}
