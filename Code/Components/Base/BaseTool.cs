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

	public virtual bool WantsSnapGrid { get; set; } = true;

	private static BaseTool _lastTool = new Sandbox.Tools.WhatIsThatTool();
	public static BaseTool Instance
	{
		get
		{
			var player = Player.FindLocalPlayer();
			if ( player == null ) return _lastTool;

			var inventory = player.Inventory;
			if ( inventory == null ) return _lastTool;

			if ( inventory.ActiveWeapon is not ToolGun tool ) return _lastTool;

			return tool?.CurrentTool;
		}
	}

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
		if ( !IsProxy )
		{
			previewModel?.Destroy();
			CreatePreview();
			Sandbox.UI.ToolMenu.Instance?.UpdateInspector();
		}
	}

	public virtual void Disabled()
	{
		if ( !IsProxy )
			previewModel?.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !IsProxy && previewModel != null )
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
		if ( !IsProxy )
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

	protected virtual bool HasModel()
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

	public void DoAxisOverlay( SceneTraceResult trace, CameraComponent camera, bool DrawLabels = true )
	{
		if ( !trace.Hit || !trace.Body.IsValid() )
			return;

		Prop prop = trace.GameObject.GetComponent<Prop>();
		Rigidbody rigidbody = trace.GameObject.GetComponent<Rigidbody>();

		if ( !prop.IsValid() || !rigidbody.IsValid() ) return;

		//Vector3 GOCenter = prop.WorldPosition + prop.Model.Bounds.Center;
		BBox bounds = rigidbody.PhysicsBody.GetBounds();

		Vector3 center = prop.WorldTransform.PointToWorld( prop.Model.Bounds.Center );

		Vector3 size = bounds.Size;
		Rotation rotation = trace.GameObject.WorldRotation;

		Vector2 goLoc = camera.PointToScreenPixels( bounds.Center, out bool _fix );

		Vector2 goWorldLoc = camera.PointToScreenPixels( prop.WorldPosition, out bool _fix2 );

		camera.Hud.DrawCircle( new Vector2( goWorldLoc.x, goWorldLoc.y ), new Vector2( 10, 10 ), Color.Magenta );
		if ( DrawLabels )
			camera.Hud.DrawText( new TextRendering.Scope( "World", Color.Magenta, 16 ), new Rect( goWorldLoc.x + 12, goWorldLoc.y - 5, 40, 10 ) );

		camera.Hud.DrawCircle( new Vector2( goLoc.x, goLoc.y ), new Vector2( 10, 10 ), Color.White );
		if ( DrawLabels )
			camera.Hud.DrawText( new TextRendering.Scope( "Center", Color.White, 16 ), new Rect( goLoc.x + 12, goLoc.y - 5, 40, 10 ) );


		DrawDirectionIndicator( camera, center, rotation.Forward, (int) (size.x / 2 * 1.5), 3, "Forward", Color.Red, DrawLabels );
		DrawDirectionIndicator( camera, center, rotation.Right,   (int) (size.y / 2 * 1.5), 3, "Right", Color.Green, DrawLabels );
		DrawDirectionIndicator( camera, center, rotation.Up,      (int) (size.z / 2 * 1.5), 3, "Up", Color.Blue, DrawLabels );
	}

	private void DrawDirectionIndicator( CameraComponent camera, Vector3 start, Vector3 direction, int length, int width, string text, Color color, bool DrawLabels = true )
	{
		Vector2 screenStart = camera.PointToScreenPixels( start, out bool _fix );
		Vector2 screenEnd = camera.PointToScreenPixels( start + (direction * length), out bool _fix2 );
		Vector2 screenText = camera.PointToScreenPixels( start + (direction * length) + new Vector3( 0, 0, 5 ), out bool _fix3 );

		camera.Hud.DrawLine( screenStart, screenEnd, width, color );
		if ( DrawLabels )
			camera.Hud.DrawText( new TextRendering.Scope( text, color, 32 ), new Rect( screenText.x, screenText.y, 40, 10 ) );
	}
}
