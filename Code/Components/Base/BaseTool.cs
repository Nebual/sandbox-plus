public abstract class BaseTool : Component
{
	public ToolGun Parent { get; set; }
	public Player Owner { get; set; }

	public string Name => DisplayInfo.For( this ).Name;
	// Shown in the tool panel
	public string Description { get => _description ?? DisplayInfo.For( this ).Description; set => _description = value; }
	private string _description = null;
	// Shown in the main HUD
	public string LongDescription { get => _longDescription ?? Description; set => _longDescription = value; }
	private string _longDescription = null;

	protected PreviewModel previewModel;
	public virtual string SpawnModel { get; set; } = "";

	public virtual bool Primary( SceneTraceResult trace )
	{
		return false;
	}

	public virtual bool Secondary( SceneTraceResult trace )
	{
		return false;
	}

	public virtual bool Reload( SceneTraceResult trace )
	{
		return false;
	}

	public virtual void Activate()
	{
		if ( previewModel != null )
		{
			previewModel.Destroy();
		}
		CreatePreview();
		if ( !IsProxy )
		{
			Sandbox.UI.ToolMenu.Instance?.UpdateInspector();
		}
	}

	public virtual void Disabled()
	{
		previewModel?.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( previewModel != null )
		{
			var trace = Parent.BasicTraceTool();
			if ( IsPreviewTraceValid( trace ) )
			{
				previewModel.Update( trace );

				if ( previewModel?.previewObject != null && previewModel.previewObject.GetComponent<ModelRenderer>() is var previewRenderer && previewRenderer.IsValid() && GetModel() != previewRenderer.Model.Name )
				{
					var model = Model.Load( GetModel() );
					previewRenderer.Model = model;
					OnUpdatePreviewModel( model );
				}
			}
			else
			{
				previewModel.SetEnabled( false );
			}
		}
	}
	protected override void OnDestroy()
	{
		base.OnDestroy();
		previewModel?.Destroy();
	}
	protected string GetConvarValue( string name, string defaultValue = null )
	{
		return ConsoleSystem.GetValue( name, defaultValue );
		// in SandboxPlus this wrapper allowed accessing client convars on the server... what does that mean in Scene system?
		// return Game.IsServer
		// 	? Owner.Client.GetClientData<string>( name, defaultValue )
		// 	: ConsoleSystem.GetValue( name, default );
	}

	protected static bool IsTraceHit( SceneTraceResult tr )
	{
		if ( !tr.Hit )
			return false;

		if ( !tr.GameObject.IsValid() )
			return false;

		return true;
	}
	// Override this if you want to show a Preview in your tool
	protected virtual bool IsPreviewTraceValid( SceneTraceResult tr )
	{
		return false;
	}

	protected bool HasModel()
	{
		if ( SpawnModel != "" ) return true;
		var toolId = GetConvarValue( "tool_current" );
		return (GetConvarValue( $"{toolId}_model" ) ?? "") != "";
	}
	protected virtual string GetModel()
	{
		if ( SpawnModel != "" ) return SpawnModel;
		var toolCurrent = GetConvarValue( "tool_current", "" );
		return GetConvarValue( $"{toolCurrent}_model" ) ?? "models/citizen_props/coffeemug01.vmdl";
	}

	public virtual void CreatePreview()
	{
		if ( !HasModel() ) return;
		previewModel = new PreviewModel
		{
			ModelPath = GetModel(),
			// PositionOffset = 0,
			RotationOffset = Rotation.From( new Angles( 90, 0, 0 ) ),
			FaceNormal = true,
		};
		previewModel.Update( Parent.BasicTraceTool() );
		OnUpdatePreviewModel( previewModel.previewObject?.GetComponent<ModelRenderer>()?.Model );
	}
	protected virtual void OnUpdatePreviewModel( Model model )
	{
	}
}
