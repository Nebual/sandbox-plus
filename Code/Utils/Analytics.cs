namespace SandboxPlus
{
	public static partial class Analytics
	{
		[ConVar( "sandboxplus_analytics", Saved = true )]
		public static bool EnableAnalytics { get; set; } = true;

		public static void Increment( string name, double amount = 1, string context = null, object data = null )
		{
			if ( !EnableAnalytics )
			{
				return;
			}
			Sandbox.Services.Stats.Increment( name, amount, context, data );
		}

		[Rpc.Host]
		public static void ServerIncrement( string name, double amount = 1, string context = null, object data = null )
		{
			Increment( name, amount, context, data );
		}
	}
}
