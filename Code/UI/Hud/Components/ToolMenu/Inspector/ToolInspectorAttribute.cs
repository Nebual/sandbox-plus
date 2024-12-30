using System;

namespace Scenebox.Tools;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public class ToolInspectorAttribute : Attribute
{
	public Type Type { get; }

	public ToolInspectorAttribute( Type type )
	{
		Type = type;
	}
}
