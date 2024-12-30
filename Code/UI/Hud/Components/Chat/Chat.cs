public partial class Chat
{
	[Rpc.Broadcast]
	public static void AddChatEntry( string name, string message, ulong playerId = 0, bool isInfo = false )
	{
		Current?.AddEntry( name, message, (long)playerId, isInfo );
		Log.Info( $"{name}: {message}" );
	}

	[ConCmd( "sandbox_say" )]
	public static void Say( string message )
	{
		// todo - reject more stuff
		if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

		if ( message.ToLower().Contains( "bloxwich" ) )
			Sandbox.Services.Achievements.Unlock( "secret_phrase" );

		// Event.Run( "client.say", ConsoleSystem.Caller, message );
		AddChatEntry( Connection.Local.DisplayName, message, Connection.Local.SteamId );
	}
}
