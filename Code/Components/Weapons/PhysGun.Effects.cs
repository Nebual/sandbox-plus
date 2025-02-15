public partial class PhysGun
{
	LegacyParticleSystem beam;
	LegacyParticleSystem endNoHit;

	GameObject lastGrabbedObject;

	[Rpc.Broadcast]
	protected virtual void KillEffects()
	{
		if ( beam.IsValid() )
		{
			beam?.GameObject?.Destroy();
			beam = null;
		}

		if ( endNoHit.IsValid() )
		{
			endNoHit?.GameObject?.Destroy();
			endNoHit = null;
		}

		DisableHighlights( lastGrabbedObject );
		lastGrabbedObject = null;
	}

	private void DisableHighlights( GameObject gameObject )
	{
		if ( gameObject.IsValid() )
		{
			foreach ( var child in gameObject.Children )
			{
				if ( !child.Components.Get<ModelRenderer>().IsValid() )
					continue;

				if ( child.Components.TryGet<HighlightOutline>( out var childglow ) )
				{
					childglow.Destroy();
				}
			}

			if ( gameObject.Components.TryGet<HighlightOutline>( out var glow ) )
			{
				glow.Destroy();
			}
		}
	}

	Vector3 lastBeamPos;

	protected virtual void UpdateEffects()
	{
		if ( !Owner.IsValid() || !Beaming )
		{
			KillEffects();
			return;
		}

		if ( grabbed && !GrabbedObject.IsValid() )
		{
			DisableHighlights( lastGrabbedObject );
		}

		var startPos = Owner.EyeTransform.Position;
		var dir = Owner.EyeTransform.Forward;

		var tr = Scene.Trace.Ray( startPos, startPos + dir * MaxTargetDistance )
			.UseHitboxes()
			.IgnoreGameObject( Owner.GameObject )
			.WithAllTags( "solid" )
			.WithoutTags( "player" )
			.Run();

		beam ??= CreateBeam( tr.EndPosition );

		if ( beam.IsValid() )
		{
			beam.WorldPosition = Attachment( "muzzle" ).Position + dir * 10f;
			beam.WorldRotation = Attachment( "muzzle" ).Rotation;
		}

		if ( GrabbedObject.IsValid() && !GrabbedObject.Tags.Contains( "world" ) && HeldBody.IsValid() )
		{
			var physGroup = HeldBody.PhysicsGroup;

			if ( physGroup != null && GrabbedBone >= 0 )
			{
				var physBody = physGroup.GetBody( GrabbedBone );
				if ( physBody != null )
				{
					beam?.SceneObject.SetControlPoint( 1, physBody.Transform.PointToWorld( GrabbedPos ) );
				}
			}
			else
			{
				if ( !HeldBody.IsValid() )
					return;

				beam?.SceneObject.SetControlPoint( 1, HeldBody.Transform.PointToWorld( GrabbedPos ) );
			}

			lastBeamPos = HeldBody.Position + HeldBody.Rotation * GrabbedPos;

			endNoHit?.GameObject.Destroy();
			endNoHit = null;

			if ( GrabbedObject.GetComponent<ModelRenderer>().IsValid() )
			{
				lastGrabbedObject = GrabbedObject;

				var glow = GrabbedObject.GetOrAddComponent<HighlightOutline>();
				glow.Width = 0.25f;
				glow.Color = new Color( 4f, 50.0f, 70.0f, 1.0f );
				glow.ObscuredColor = new Color( 4f, 50.0f, 70.0f, 0.0005f );

				foreach ( var child in lastGrabbedObject.Children )
				{
					if ( !child.GetComponent<ModelRenderer>().IsValid() )
						continue;

					glow = child.GetOrAddComponent<HighlightOutline>();
					glow.Color = new Color( 0.1f, 1.0f, 1.0f, 1.0f );
				}
			}
		}
		else
		{
			lastBeamPos = tr.EndPosition;

			Vector3.Lerp( lastBeamPos, tr.EndPosition, Time.Delta * 10 );

			if ( beam?.IsValid() == true )
				beam?.SceneObject.SetControlPoint( 1, lastBeamPos );

			endNoHit ??= Particles.MakeParticleSystem( "particles/physgun_end_nohit.vpcf", new Transform( lastBeamPos ), 0 );
			endNoHit.SceneObject.SetControlPoint( 0, lastBeamPos );
			endNoHit.WorldPosition = lastBeamPos;
		}
	}

	private LegacyParticleSystem CreateBeam( Vector3 endPos ) =>
		Particles.MakeParticleSystem( "particles/physgun_beam.vpcf", new Transform( endPos ), 0 );

	private void FreezeEffects() =>
		Particles.MakeParticleSystem( "particles/physgun_freeze.vpcf", new Transform( lastBeamPos ), 4 );

	protected override void OnDestroy()
	{
		base.OnDestroy();
		KillEffects();
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		if ( channel == Owner.Network.Owner && this.IsValid() )
			KillEffects();
	}
}
