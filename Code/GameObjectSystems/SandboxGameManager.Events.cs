using Sandbox.Events;

public partial class SandboxGameManager : IPropSpawnedEvent
{
	public void OnSpawned( Prop prop )
	{
		prop.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		prop.Network.SetOrphanedMode( NetworkOrphaned.Host );
	}
}
