namespace Sandbox
{
	public class PlayerSettings
	{
		public Player player; // not saved
		private bool useHumanModel = false;
		private bool isDirty = false;

		// any Properties will be saved
		public bool UseHumanModel
		{
			get => useHumanModel;
			set
			{
				if ( useHumanModel == value ) return;
				useHumanModel = value;
				SetDirty();
			}
		}

		public static string CustomCrosshairDefault = "(circle;255;255;255;0;0;5;5)";
		private string customCrosshair = CustomCrosshairDefault;
		public string CustomCrosshair
		{
			get => customCrosshair;
			set
			{
				if ( customCrosshair == value ) return;
				customCrosshair = value;
				SetDirty();

				if (player != null) player.RegenerateCrosshair();
			}
		}

		// This is a bit overengineered heh; outer code can just set Player.Local.Settings.UseHumanModel = true, and after 5 seconds it will be saved.
		// Multiple writes to PlayerSettings in that period will be batched together, and SetDirty(false) will cancel the save.
		private void SetDirty( bool dirty = true )
		{
			if ( !isDirty && dirty )
			{
				isDirty = true;
				GameTask.DelaySeconds( 5 ).ContinueWith( ( _ ) =>
				{
					if ( isDirty )
					{
						Save();
						isDirty = false;
					}
				} );
			}
			else if ( !dirty )
			{
				isDirty = false;
			}
		}

		public static void Save()
		{
			var data = Player.Local.Settings;
			FileSystem.Data.WriteJson( "playersettings.json", data );
		}
		public static PlayerSettings Load()
		{
			var loaded = FileSystem.Data.ReadJson<PlayerSettings>( "playersettings.json" ) ?? new PlayerSettings();
			loaded.player = Player.FindLocalPlayer();
			loaded.SetDirty( false );
			return loaded;
		}
	}
}
