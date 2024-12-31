namespace Sandbox.Events;

public interface IPropSpawnedEvent : ISceneEvent<IPropSpawnedEvent>
{
	void OnSpawned( Prop prop ) { }
}
