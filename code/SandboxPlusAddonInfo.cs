using System;
using System.Collections.Generic;
using MinimalExtended;
using Sandbox;

namespace SandboxGame
{
	[Library( "sandbox-info" )]
	public class AddonInfo : BaseAddonInfo
	{
		public override string Name => "SandboxPlus";

		public override string Description => "Sample playground gamemode";

		public override string Author => "Garry";

		public override double Version => 1.0;

		public override List<AddonDependency> Dependencies => new()
		{
			new AddonDependency() {
				Name = "Logger",
				MinVersion = 1.0
			}
		};
	}
	public class SandboxAddon : AddonClass<AddonInfo> { }
}
