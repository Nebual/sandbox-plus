using System.Security.AccessControl;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PlayerCrosshair
{
	public class HudObject 
	{
		public virtual void Draw( CameraComponent camera, Vector2 centerPos ) { }
	}
	public class Circle : HudObject
	{
		Color Color { get; set; } = Color.White;
		Vector2 Location { get; set; } = Vector2.Zero;
		Vector2 Size { get; set; } = Vector2.Zero;

		public Circle( Color color, Vector2 loc, Vector2 size )
		{
			Color = color;
			Location = Location;
			Size = size;
		}

		public override void Draw( CameraComponent camera, Vector2 centerPos)
		{
			camera.Hud.DrawCircle(centerPos + Location, Size, Color);
		}
	}
	public class Line : HudObject
	{
		Color Color { get; set; } = Color.White;
		Vector2 Start { get; set; } = Vector2.Zero;
		Vector2 End { get; set; } = Vector2.Zero;
		int Thickness { get; set; } = 0;

		public Line( Color color, Vector2 startLoc, Vector2 endLoc, int thickness )
		{
			Color = color;
			Start = startLoc;
			End = endLoc;
			Thickness = thickness;
		}


		public override void Draw( CameraComponent camera, Vector2 centerPos )
		{
			camera.Hud.DrawLine( centerPos + Start, centerPos + End, Thickness, Color );
		}
	}
	public class Rectangle : HudObject
	{
		Color Color { get; set; } = Color.White;
		int Left { get; set; } = 0;
		int Top { get; set; } = 0;
		int Width { get; set; } = 0;
		int Height { get; set; } = 0;
		int Thickness { get; set; } = 0;

		public Rectangle( Color color, int left, int top, int width, int height, int thickness )
		{
			Color = color;
			Left = left;
			Top = top;
			Width = width;
			Height = height;
			Thickness = thickness;
		}


		public override void Draw( CameraComponent camera, Vector2 centerPos )
		{
			camera.Hud.DrawRect( new Rect( centerPos.x - Left, centerPos.y - Top, Width, Height ), Color, borderWidth: new Vector4( Thickness ) );
		}
	}


	public class Crosshair
	{
		private static string Pattern = @"\((.*?)\)";
		public static List<HudObject> ConfigToHudObjects( string config )
		{
			List<HudObject> objects = new();

			foreach ( Match match in Regex.Matches( config, Pattern ) )
			{
				string group = match.Groups[1].Value;
				var items = new List<string>( group.Split( ';' ) );

				string type = items.First();

				List<int> values = new();
				for(int i = 1; i < items.Count; i++)
				{
					if ( int.TryParse( items[i], out var val ) ) values.Add( val );
				}

				if (type == "circle" && values.Count >= 7 )
				{
					// (circle;255;255;255;-2;-2;4;4)
					objects.Add(
						new Circle( new Color( values[0], values[1], values[2] ), new Vector2( values[3], values[4] ), new Vector2( values[5], values[6] ) )
					);
				}
				else if (type == "line" && values.Count >= 8 )
				{
					// (line;255;255;255;-25;-25;-4;-4;1)
					objects.Add(
						new Line( new Color( values[0], values[1], values[2] ), new Vector2( values[3], values[4] ), new Vector2( values[5], values[6] ), values[7] )
					);
				}
				else if (type == "rect")
				{
					if (values.Count >= 9)
					{
						// (rect;255;255;0;-25;-25;50;50;1;1) Solid square
						if ( values[8] == 1 )
						{
							objects.Add(
								new Rectangle( new Color( values[0], values[1], values[2] ), values[3], values[4], values[5], values[6], values[7] )
							);
						}
						else
						{
							// (rect;255;255;0;-25;-25;50;50;1;1) Hollow square 
							// Left, Top, Width, Height	
							// Left: values[3]
							// Top: values[4]
							// Width: values[5]
							// Height: values[6]

							// Top left: values[3], values[4]
							// Top right: values[3] + values[5], values[4]
							// Bottom left: values[3], values[4] + values[6]
							// Bottom right: values[3] + values[5], values[4] + values[6]

							objects.Add( new Line( new Color( values[0], values[1], values[2] ), new Vector2( values[3], values[4] ), new Vector2( values[3] + values[5], values[4] ), values[7] ) );
							objects.Add( new Line( new Color( values[0], values[1], values[2] ), new Vector2( values[3], values[4] ), new Vector2( values[3], values[4] + values[6] ), values[7] ) );
							objects.Add( new Line( new Color( values[0], values[1], values[2] ), new Vector2( values[3] + values[5], values[4] + values[6] ), new Vector2( values[3], values[4] + values[6] ), values[7] ) );
							objects.Add( new Line( new Color( values[0], values[1], values[2] ), new Vector2( values[3] + values[5], values[4] + values[6] ), new Vector2( values[3] + values[5], values[4] ), values[7] ) );
						}
					}
					else
					{

					}
				}

			}

			return objects;
		}
	}
}
