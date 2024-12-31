namespace SandboxPlus;

public sealed partial class PlayerController : Component
{
	[Property, Feature( "Snapping" )]
	public bool EnableSnapping { get; set; } = true;

	[Property, Feature( "Snapping" )]
	public bool EnableSnapHud { get; set; } = true;
	
	[Property, Feature( "Snapping" )]
	public bool EnableSnapHudOnStatic { get; set; } = true;
	
	[Property, Feature( "Snapping" )]
	public bool EnableSnapHudBounds { get; set; } = true;
	
	[Property, Feature( "Snapping" )]
	public bool EnableSnapHudGrid { get; set; } = true;
	
	[Property, Feature( "Snapping" )]
	public int SnapGridRows { get; set; } = 16;
	
	[Property, Feature( "Snapping" )]
	public int SnapGridColumns { get; set; } = 16;
	
	private Vector3[] CachedSnapCorners { get; set; }

	private int CachedFocusedFace { get; set; } = 0;

	private int[] CachedFaceLines { get; set; } = {0, 1, 2, 3};
	
	private int[] CachedFaceCorners { get; set; } = {0, 1, 2, 3};

	private readonly int[][] LinesForFace =
	[
		// top face
		[0, 1, 2, 3],
		
		// bottom face
		[4, 5, 6, 7],

		// left face
		[0, 4, 8, 9],
		
		// right face
		[2, 6, 10, 11],
		
		// front face
		[1, 9, 5, 10],
		
		// back face
		[3, 7, 11, 8]
		
	];

	private readonly int[][] CornersForFace =
	[
		// top face
		[0, 1, 3, 2],
		
		// bottom face
		[4, 5, 7, 6],
		
		// left face
		[4, 5, 0, 1],
		
		// right face
		[6, 7, 2, 3],
		
		// front face
		[5, 6, 1, 2],
		
		// back face
		[7, 4, 3, 0],
	];

	private readonly Vector3[] FaceDirection =
	[
		Vector3.Up,
		Vector3.Down,
		Vector3.Left,
		Vector3.Right,
		Vector3.Backward,
		Vector3.Forward,
	];
	
	[Property, Feature( "Snapping" )]
	public Color SnapUnfocusedLineColor = new Color( 40, 40, 80, 0.2f );
	
	[Property, Feature( "Snapping" )]
	public Color SnapFocusedLineColor = new Color( 150, 150, 150, 0.4f );

	private bool IsSnapping { get; set; } = false;
	
	private bool IsShowingSnapHUD { get; set; } = false;
	
	private Vector3? SnapViewPlaneIntersection;
	
	private Vector3? SnapLocation;

	void OnRenderSnapHUD()
	{
		IsShowingSnapHUD = false;
		
		if ( !EnableSnapping || !EnableSnapHud )
			return;

		if ( Hovered != null )
		{
			if ( Hovered.GetComponent<Prop>() is { } prop )
			{
				if ( !EnableSnapHudOnStatic && prop.IsStatic )
					return;
				
				UpdateCachedSnapData(prop.Model.PhysicsBounds, prop.WorldTransform);

				DrawSnapHud( prop );
			}
		}
	}

	void UpdateSnapInput()
	{
		IsSnapping = false;
		
		if ( !IsShowingSnapHUD )
			return;

		if ( !SnapLocation.HasValue )
			return;

		if ( !Hovered.IsValid() )
			return;

		if ( Input.Down( UseButton ) )
		{
			EyeAngles = Vector3.Direction( Scene.Camera.WorldPosition, SnapLocation.Value ).EulerAngles;
			IsSnapping = true;
		}
	}

	void UpdateCachedSnapData(BBox bounds, Transform worldTransform)
	{
		// Don't update the snap data while snapping
		// It causes crazy camera things to happen
		if ( IsSnapping )
			return;
		
		var mins = bounds.Mins;
		var maxs = bounds.Maxs;
		
		CachedSnapCorners =
		[
			worldTransform.PointToWorld(new Vector3( mins.x, mins.y, mins.z )), worldTransform.PointToWorld(new Vector3( maxs.x, mins.y, mins.z )),
			worldTransform.PointToWorld(new Vector3( maxs.x, maxs.y, mins.z )), worldTransform.PointToWorld(new Vector3( mins.x, maxs.y, mins.z )),
			worldTransform.PointToWorld(new Vector3( mins.x, mins.y, maxs.z )), worldTransform.PointToWorld(new Vector3( maxs.x, mins.y, maxs.z )),
			worldTransform.PointToWorld(new Vector3( maxs.x, maxs.y, maxs.z )), worldTransform.PointToWorld(new Vector3( mins.x, maxs.y, maxs.z ))
		];
		
		// determine which face we're looking at
		var viewRay = Scene.Camera.ScreenNormalToRay(new Vector3(0.5f, 0.5f, 0f));
		var traceResult = Scene.Trace.Ray( viewRay, 1500f ).IgnoreGameObject( GameObject ).Run();
		var hitNormal = -worldTransform.NormalToLocal( traceResult.Normal );
		var closestMatch = 0f;
		for ( var i = 0; i < 6; i++ )
		{
			var dot = FaceDirection[i].Dot(hitNormal);
			if ( dot > closestMatch )
			{
				CachedFocusedFace = i;
				closestMatch = dot;
			}
		}
		
		CachedFaceLines = LinesForFace[CachedFocusedFace];
		CachedFaceCorners = CornersForFace[CachedFocusedFace];
		
		var topLeftCorner = CornersForFace[CachedFocusedFace][0];
		var topRightCorner = CornersForFace[CachedFocusedFace][1];
		var bottomLeftCorner = CornersForFace[CachedFocusedFace][2];
		var bottomRightCorner = CornersForFace[CachedFocusedFace][3];
		var topLeft = CachedSnapCorners[topLeftCorner];
		var topRight = CachedSnapCorners[topRightCorner];
		var bottomLeft = CachedSnapCorners[bottomLeftCorner];
		var bottomRight = CachedSnapCorners[bottomRightCorner];
		
		// determine where we're aiming on snap plane
		viewRay = viewRay.ToLocal( worldTransform );
		var plane = new Plane(worldTransform.PointToLocal(topLeft), -FaceDirection[CachedFocusedFace]);

		SnapViewPlaneIntersection = plane.Trace( viewRay, false );
		if ( !SnapViewPlaneIntersection.HasValue )
		{
			SnapLocation = null;
			return;
		}
	}

	void DrawSnapHud( Prop hoveredProp )
	{
		if ( EnableSnapHudBounds )
		{
			DrawBounds();
		}

		if ( EnableSnapHudGrid )
		{
			IsShowingSnapHUD = true;
			DrawGrid(hoveredProp);
		}
	}
	
	void DrawBounds()
	{
		DrawLine(CachedSnapCorners[0], CachedSnapCorners[1], CachedFaceLines.Contains(0) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[1], CachedSnapCorners[2], CachedFaceLines.Contains(1) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[2], CachedSnapCorners[3], CachedFaceLines.Contains(2) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[3], CachedSnapCorners[0], CachedFaceLines.Contains(3) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[4], CachedSnapCorners[5], CachedFaceLines.Contains(4) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[5], CachedSnapCorners[6], CachedFaceLines.Contains(5) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[6], CachedSnapCorners[7], CachedFaceLines.Contains(6) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[7], CachedSnapCorners[4], CachedFaceLines.Contains(7) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[0], CachedSnapCorners[4], CachedFaceLines.Contains(8) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[1], CachedSnapCorners[5], CachedFaceLines.Contains(9) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[2], CachedSnapCorners[6], CachedFaceLines.Contains(10) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
		DrawLine(CachedSnapCorners[3], CachedSnapCorners[7], CachedFaceLines.Contains(11) ? SnapFocusedLineColor : SnapUnfocusedLineColor, 3);
	}

	void DrawGrid(Prop hoveredProp)
	{
		var topLeftCorner = CornersForFace[CachedFocusedFace][0];
		var topRightCorner = CornersForFace[CachedFocusedFace][1];
		var bottomLeftCorner = CornersForFace[CachedFocusedFace][2];
		var bottomRightCorner = CornersForFace[CachedFocusedFace][3];
		
		var topLeft = CachedSnapCorners[topLeftCorner];
		var topRight = CachedSnapCorners[topRightCorner];
		var bottomLeft = CachedSnapCorners[bottomLeftCorner];
		var bottomRight = CachedSnapCorners[bottomRightCorner];

		var closestRowToAimPos = 0;
		var closestColToAimPos = 0;

		var world = hoveredProp.WorldTransform;

		if ( SnapViewPlaneIntersection.HasValue )
		{
			var localTopLeft = world.PointToLocal(topLeft);
			var localTopRight = world.PointToLocal(topRight);
			var localBottomRight = world.PointToLocal(bottomRight);
			var localBottomLeft = world.PointToLocal(bottomLeft);
			
			var intersection = SnapViewPlaneIntersection.Value;
			var intersectingPoint = intersection;
			var closestIntersection = 1000000f;
			for ( var row = 0; row <= SnapGridRows; row++ )
			{
				var rowFrac = (float)row / SnapGridRows;
				var rowCenter = (Vector3.Lerp( localTopLeft, localBottomLeft, rowFrac ) + Vector3.Lerp( localTopRight, localBottomRight, rowFrac ));
				for ( var col = 0; col <= SnapGridColumns; col++ )
				{
					var colFrac = (float)col / SnapGridColumns;
					var colCenter = (Vector3.Lerp( localTopLeft, localTopRight, colFrac ) + Vector3.Lerp( localBottomLeft, localBottomRight, colFrac ));
					var point = (rowCenter + colCenter) * 0.5f;
					var dist = intersection.Distance( point );
					if ( dist < closestIntersection )
					{
						closestRowToAimPos = row;
						closestColToAimPos = col;
						closestIntersection = dist;
						intersectingPoint = point;
					}
				}
			}
			
			var worldTransform = hoveredProp.WorldTransform;
			var HitResult = Scene.Trace
				.Ray( worldTransform.PointToWorld((FaceDirection[CachedFocusedFace] * -1f) + intersectingPoint),
					worldTransform.PointToWorld((FaceDirection[CachedFocusedFace] * 99999f) + intersectingPoint) )
				.Run();

			if ( !HitResult.Hit )
			{
				SnapLocation = null;
				return;
			}

			SnapLocation = HitResult.HitPosition;
		}

		for ( var row = 1; row < SnapGridRows; row++ )
		{
			var isRowFocused = row == closestRowToAimPos;
			DrawLine(
				Vector3.Lerp(topLeft, bottomLeft, (float)row / SnapGridRows), 
				Vector3.Lerp(topRight, bottomRight, (float)row / SnapGridRows),
				isRowFocused ? SnapFocusedLineColor : SnapUnfocusedLineColor,
				isRowFocused ? 4 : 2
			);
		}
		
		for ( var col = 1; col < SnapGridColumns; col++ )
		{
			var isColFocused = col == closestColToAimPos;
			DrawLine(
				Vector3.Lerp(topLeft, topRight, (float)col / SnapGridColumns), 
				Vector3.Lerp(bottomLeft, bottomRight, (float)col / SnapGridColumns),
				isColFocused ? SnapFocusedLineColor : SnapUnfocusedLineColor,
				isColFocused ? 4 : 2
			);
		}
	}

	void DrawLine( Vector3 start, Vector3 end, Color lineColor, float width = 2, string text = null )
	{
		var camera = Scene.Camera;
		var hud = camera.Hud;
		var screenStart = camera.PointToScreenPixels( start );
		var screenEnd = camera.PointToScreenPixels( end );
		
		hud.DrawLine( screenStart, screenEnd, width, lineColor);
		
		if ( text != null )
		{
			var textPos = camera.PointToScreenPixels( (start + end) / 2 );
			var textScope = TextRendering.Scope.Default;
			textScope.Text = text;
			textScope.TextColor = lineColor;
			textScope.FontSize = 24f;
			
			hud.DrawText(textScope, textPos);
		}
	}
}
