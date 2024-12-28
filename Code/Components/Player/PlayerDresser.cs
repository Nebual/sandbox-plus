public sealed class PlayerDresser : Component, Component.INetworkSpawn
{
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }

	// On initial connect/spawn, OnNetworkSpawn is called on the host
	// On subsequent respawns, OnNetworkSpawn is called on the client, so we need to RPC to get the clothing from the host
	public async void OnNetworkSpawn( Connection owner )
	{
		await GameTask.Delay(100); // wait for the new Player object to be created on the host
		LoadClothes(owner.Id);
	}
	[Rpc.Host]
	private static void LoadClothes(Guid connectionId)
	{
		var owner = Connection.Find(connectionId);
		var serializedClothes = owner.GetUserData("avatar"); // only host knows this
		Player.FindPlayerByConnection(owner)
			.GetComponentInChildren<PlayerDresser>()
			.ApplyClothes(serializedClothes);
	}
	[Rpc.Broadcast]
	public void ApplyClothes(string serializedClothes)
	{
		var clothing = new ClothingContainer();
		clothing.Deserialize( serializedClothes );
		clothing.Apply(BodyRenderer);
	}
}
