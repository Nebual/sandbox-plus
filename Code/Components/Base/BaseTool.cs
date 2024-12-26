public abstract class BaseTool : Component
{
	public ToolGun Parent { get; set; }
	public Player Owner { get; set; }

	// Set this to override the [Library]'s class default
	public string Description { get; set; } = null;

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
		SpawnMenu.Instance?.ToolPanel?.DeleteChildren( true );
		CurrentTool.CreateToolPanel();
	}

	public virtual void Disabled()
	{
	}

	public virtual void CreateToolPanel()
	{
	}

	protected string GetConvarValue( string name, string defaultValue = null )
	{
		return ConsoleSystem.GetValue( name, defaultValue );
		// in SandboxPlus this wrapper allowed accessing client convars on the server... what does that mean in Scene system?
		// return Game.IsServer
		// 	? Owner.Client.GetClientData<string>( name, defaultValue )
		// 	: ConsoleSystem.GetValue( name, default );
	}
}
