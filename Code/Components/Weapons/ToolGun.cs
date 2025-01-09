[Library( "weapon_tool", Title = "Toolgun" )]
public partial class ToolGun : BaseWeapon
{
	[ConVar( "tool_current" )] public static string UserToolCurrent { get; set; } = "tool_boxgun";

	public BaseTool CurrentTool { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();
		UpdateTool();
		SetupToolgunPanel();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		CurrentTool?.Disabled();
		DestroyToolgunPanel();
	}

	public override bool WantsSnapGrid()
	{
		if ( CurrentTool != null )
		{
			return CurrentTool.WantsSnapGrid;
		}

		return true;
	}

	string lastTool;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( lastTool != UserToolCurrent )
		{
			UpdateTool();
		}
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateEffects();
		UpdateToolgunPanel();
	}

	public override void AttackPrimary()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Primary( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition, trace.Normal );
	}

	public override void AttackSecondary()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Secondary( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition, trace.Normal );
	}

	public override void Reload()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Reload( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition, trace.Normal );
	}

	public void UpdateTool()
	{
		var comp = TypeLibrary.GetType( UserToolCurrent );

		if ( comp == null )
			return;

		lastTool = UserToolCurrent;

		Components.Create( comp, true );

		CurrentTool?.Destroy();

		CurrentTool = Components.Get<BaseTool>();
		CurrentTool.Owner = Owner;
		CurrentTool.Parent = this;
		CurrentTool.Activate();
	}

	public SceneTraceResult TraceTool( Vector3 start, Vector3 end, float radius = 0 )
	{
		var trace = Scene.Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "npc", "glass" )
				.WithoutTags( "debris", "player" )
				.IgnoreGameObjectHierarchy( Owner.GameObject );

		if (radius > 0)
			trace = trace.Size( radius );

		var tr = trace.Run();

		return tr;
	}

	public SceneTraceResult BasicTraceTool()
	{
		return TraceTool( Owner.AimRay.Position, Owner.AimRay.Position + Owner.AimRay.Forward * 5000 );
	}
}
