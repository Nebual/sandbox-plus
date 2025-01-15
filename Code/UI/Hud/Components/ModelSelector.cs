using Sandbox.UI.Tests;
using System.Text.RegularExpressions;
using Sandbox.UI.Construct;

namespace Sandbox.UI
{
	[Library]
	public partial class ModelSelector : Panel
	{
		private static Dictionary<string, HashSet<string>> SpawnLists = new();
		private static bool spawnListsLoaded = false;
		VirtualScrollPanel Canvas;

		private static readonly Regex reModelMatGroup = new( @"^(.*?)(?:--(\d+))?(\.vmdl)?$" );
		private static readonly Regex reSpawnlistFile = new( @"([^\.]+)\.spawnlist$" );

		public IEnumerable<string> SpawnListNames { get; set; }
		public Action<string> OnValueChanged { get; set; }
		protected string Value { get; set; }
		public SerializedProperty SerializedProperty
		{
			get => _property;
			set
			{
				_property = value;
				Value = _property.GetValue<string>();
			}
		}
		SerializedProperty _property;
		protected string ValueMaterialGroup { get; set; }
		public SerializedProperty SerializedPropertyMaterialGroup
		{
			get => _propertyMaterialGroup;
			set
			{
				_propertyMaterialGroup = value;
				ValueMaterialGroup = _propertyMaterialGroup.GetValue<string>();
			}
		}
		SerializedProperty _propertyMaterialGroup;

		private bool initialized;
		protected override void OnParametersSet()
		{
			if (initialized) return;
			initialized = true;
			AddClass( "modelselector" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.AutoColumns = true;
			Canvas.Layout.ItemWidth = 64;
			Canvas.Layout.ItemHeight = 64;
			Canvas.OnCreateCell = async ( cell, data ) =>
			{
				var file = (string)data;
				if ( file == null ) return;
				Panel panel;

				if ( FileSystem.Mounted.FileExists( file + "_c.png" ) )
				{
					panel = cell.Add.Image( $"/{file}_c.png", "icon" );
				}
				else if ( file.EndsWith( ".vmdl" ) )
				{
					// Mounted Cloud models don't have vmdl_c.png spawnicons, so we have to render them ourselves
					var sceneWorld = new SceneWorld();
					var sceneLight = new SceneLight( sceneWorld, Vector3.Up * 75.0f, 200.0f, Color.White * 20.0f );

					// todo: how does the Model devtool calculate a nice angle for its autogenerated spawnicons?
					var sceneModel = new SceneModel( sceneWorld, file, new Transform( Vector3.Zero, new Rotation( new Vector3( -0.25f, 0.25f, 0.9f ), 0f ) ) );

					panel = cell.Add.ScenePanel( sceneWorld, Vector3.Up * (sceneModel.Model.Bounds.Size.Length + 10), new Angles( 90, 0, 0 ).ToRotation(), 45, "icon" );
					(panel as ScenePanel).RenderOnce = true;
				}
				else
				{
					panel = await AddCloudModelIcon( cell, file );
				}

				var match = reModelMatGroup.Match( file );
				panel.AddEventListener( "onclick", () =>
				{
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					var model = match.Groups[1].Value + match.Groups[3].Value;
					if ( Input.Down( "run" ) )
					{
						Clipboard.SetText( model );
						Log.Info( $"Copied to clipboard: {model}" );
						return;
					}
					if ( match.Groups[3].Value == ".vmdl" )
					{
						SetToolModelClient( currentTool, model, match.Groups[2].Value );
					}
					else
					{
						SetToolCloudModel( currentTool, match.Groups[1].Value + match.Groups[3].Value, match.Groups[2].Value );
					}
				} );
			};

			var spawnList = SpawnListNames.SelectMany( GetSpawnList );

			foreach ( var file in spawnList )
			{
				Canvas.AddItem( file );
			}
			// VirtualScrollPanel doesn't have a valid height (subsequent children overlap it within flex-direction: column) so calculate it manually
			Style.Height = (64 + 6) * (int)Math.Ceiling( spawnList.Count() / 5f );
		}

		private async Task<Panel> AddCloudModelIcon( Panel cell, string file )
		{
			var package = await Package.FetchAsync( file, true, true );
			if ( package == null )
			{
				Log.Warning( $"ModelSelector: Tried to load model package {file} - which was not found" );
				return null;
			}

			var panel = cell.Add.Image( package.Thumb, "icon" );
			return panel;
		}

		public async void SetToolCloudModel( string tool, string model, string materialGroup )
		{
			if ( model == "" || model.EndsWith( ".vmdl" ) )
				return;

			var package = await Package.FetchAsync( model, false, true );
			if ( package == null )
			{
				Log.Warning( $"ModelSelector.Mount: Tried to load model package {model} - which was not found" );
				return;
			}

			await SandboxGameManager.BroadcastMount( package );
			model = package.GetCachedMeta( "SingleAssetSource", "" );
			if ( model == "" )
			{
				Log.Warning( $"ModelSelector.Mount: package {model} lacks SingleAssetSource - is it actually a model?" );
				return;
			}

			SetToolModelClient( tool, model, materialGroup );
		}

		public void SetToolModelClient( string tool, string model, string materialGroup )
		{
			Value = model;
			OnValueChanged?.Invoke( Value );
			_property?.SetValue( Value );
			ValueMaterialGroup = materialGroup;
			_propertyMaterialGroup?.SetValue( ValueMaterialGroup );

			if ( ConsoleSystem.GetValue( $"{tool}_model" ) != null )
			{
				ConsoleSystem.Run( $"{tool}_model", model );
			}
			if ( ConsoleSystem.GetValue( $"{tool}_materialgroup" ) != null )
			{
				ConsoleSystem.Run( $"{tool}_materialgroup", materialGroup );
			}
		}

		/// To add models/materials to the spawnlists:
		/// either call these functions in your addon init, like `ModelSelector.AddToSpawnlist( "thruster", new string[] {"models/blah.vmdl"} )`
		/// or add an `addonname.thruster.spawnlist` file (newline delimited list of models)
		public static void AddToSpawnlist( string list, string model )
		{
			SpawnLists.GetOrCreate( list ).Add( model );
		}
		public static void AddToSpawnlist( string list, IEnumerable<string> models )
		{
			SpawnLists.GetOrCreate( list ).UnionWith( models );
		}

		public static IEnumerable<string> GetSpawnList( string list )
		{
			if ( !spawnListsLoaded )
			{
				InitializeSpawnlists();
			}
			return SpawnLists.GetOrCreate( list );
		}

		[ConCmd( "reload_spawnlists" )]
		public static void InitializeSpawnlists()
		{
			SpawnLists.Clear();
			spawnListsLoaded = true;
			foreach ( var file in FileSystem.Mounted.FindFile( "/", "*.spawnlist", true ) )
			{
				var match = reSpawnlistFile.Match( file );
				var listName = match.Groups[1].Value;
				var models = FileSystem.Mounted.ReadAllText( file ).Trim().Split( '\n' ).Select( x => x.Trim() );
				AddToSpawnlist( listName, models );
			}
			// formerly Event.Run( "spawnlists.initialize" );
			foreach ( var method in TypeLibrary.FindStaticMethods( "SpawnlistsInitialize" ) )
			{
				method.Invoke( null );
			}
		}
	}
}
