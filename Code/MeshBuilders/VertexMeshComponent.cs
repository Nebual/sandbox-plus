namespace Sandbox
{
	public partial class VertexMeshComponent : Prop, IDuplicatable
	{
		[Sync]
		public string ModelId { get; set; }
		public Model VertexModel => VertexMeshBuilder.Models[ModelId];

		private string _lastModel;

		protected override void OnUpdate()
		{
			if ( ModelId == "" || ModelId == null ) return;

			if ( ModelId != "" && ModelId != _lastModel )
			{
				if ( !VertexMeshBuilder.Models.ContainsKey( ModelId ) )
				{
					VertexMeshBuilder.CreateModel( ModelId );
				}
				Model = VertexModel;
				_lastModel = ModelId;
			}
		}

		Dictionary<string, object> IDuplicatable.PreDuplicatorCopy()
		{
			return new()
			{
				{ "ModelId", ModelId }
			};
		}
		void IDuplicatable.PostDuplicatorPaste( Dictionary<string, object> saved )
		{
			ModelId = (string)saved["ModelId"];
			OnUpdate();
			GetComponent<Rigidbody>().PhysicsBody.BodyType = PhysicsBodyType.Static;
		}
	}
}
