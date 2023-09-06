namespace Sandbox
{
	public static partial class Analytics
	{
		[ConVar.Client( "sandboxplus_analytics", Saved = true )]
		public static bool EnableAnalytics { get; set; } = true;

		public static void Increment( string name, double amount = 1, string context = null, object data = null )
		{
			if ( !Game.IsClient )
			{
				return;
			}
			if ( !EnableAnalytics )
			{
				return;
			}
			Sandbox.Services.Stats.Increment( name, amount, context, data );
		}

		[ClientRpc]
		public static void ServerIncrement( string name, double amount = 1, string context = null, object data = null )
		{
			Increment( name, amount, context, data );
		}
	}
}
