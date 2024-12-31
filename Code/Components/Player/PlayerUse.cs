public sealed class PlayerUse : Component, SandboxPlus.PlayerController.IEvents
{
	[RequireComponent] public Player Player { get; set; }

	void SandboxPlus.PlayerController.IEvents.FailPressing()
	{
		BroadcastFailPressing();
	}

	[Rpc.Broadcast]
	private void BroadcastFailPressing()
	{
		Sound.Play( "player_use_fail", WorldPosition );
	}
}
