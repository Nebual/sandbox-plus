namespace Sandbox
{
	public partial class VertexMeshComponent : Prop
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
	}
}
