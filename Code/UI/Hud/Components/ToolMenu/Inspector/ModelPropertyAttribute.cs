namespace Sandbox.Tools;

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter )]
public class ModelPropertyAttribute : System.Attribute
{
	public string[] SpawnLists { get; set; }
}
