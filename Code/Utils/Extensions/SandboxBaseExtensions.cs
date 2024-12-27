/// <summary>
/// Extensions for Surfaces
/// </summary>
public static partial class SandboxBaseExtensions
{
	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit
	/// </summary>
	private static LegacyParticleSystem DoImpact( 
		this Surface self, 
		SceneTraceResult tr,
		Func<Surface.ImpactEffectData, List<string>> getDecals,
		Func<Surface.ImpactEffectData, List<string>> getEffects,
		Func<Surface.SoundData, string> getSound
		)
	{
		var surf = self.GetBaseSurface();

		//
		// Drop a decal
		//
		if ( getDecals != null )
		{
			var decalPath = Game.Random.FromList( getDecals( self.ImpactEffects ) );

			while ( string.IsNullOrWhiteSpace( decalPath ) && surf != null )
			{
				decalPath = Game.Random.FromList( getDecals( surf.ImpactEffects ) );
				surf = surf.GetBaseSurface();
			}

			if ( !string.IsNullOrWhiteSpace( decalPath ) )
			{
				if ( ResourceLibrary.TryGet<DecalDefinition>( decalPath, out var decal ) )
				{
					var go = new GameObject
					{
						Name = decalPath,
						Parent = tr.GameObject,
						WorldPosition = tr.EndPosition,
						WorldRotation = Rotation.LookAt( -tr.Normal )
					};

					if ( tr.Bone > -1 )
					{
						var renderer = tr.GameObject.GetComponentInChildren<SkinnedModelRenderer>();
						var bone = renderer.GetBoneObject( tr.Bone );

						go.SetParent( bone );
					}

					var randomDecal = Game.Random.FromList( decal.Decals );

					var decalRenderer = go.AddComponent<DecalRenderer>();
					decalRenderer.Material = randomDecal.Material;
					decalRenderer.Size = new Vector3( randomDecal.Width.GetValue(), randomDecal.Height.GetValue(), randomDecal.Depth.GetValue() );

					go.NetworkSpawn( null );
					go.Network.SetOrphanedMode( NetworkOrphaned.Host );
					go.DestroyAsync( 10f );
				}
			}
		}

		//
		// Make an impact sound
		//
		if ( getSound != null )
		{
			var sound = getSound( self.Sounds );

			surf = self.GetBaseSurface();
			while ( string.IsNullOrWhiteSpace( sound ) && surf != null )
			{
				sound = getSound( surf.Sounds );
				surf = surf.GetBaseSurface();
			}

			if ( !string.IsNullOrWhiteSpace( sound ) )
			{
				BroadcastDoBulletImpact( sound, tr.EndPosition );
			}
		}

		//
		// Get us a particle effect
		//
		if ( getEffects != null )
		{

			string particleName = Game.Random.FromList( getEffects( self.ImpactEffects ) );
			if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromList( self.ImpactEffects.Regular );

			surf = self.GetBaseSurface();
			while ( string.IsNullOrWhiteSpace( particleName ) && surf != null )
			{
				particleName = Game.Random.FromList( getEffects( surf.ImpactEffects ) );
				if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromList( surf.ImpactEffects.Regular );

				surf = surf.GetBaseSurface();
			}

			if ( !string.IsNullOrWhiteSpace( particleName ) )
			{
				var go = new GameObject
				{
					Name = particleName,
					Parent = tr.GameObject,
					WorldPosition = tr.EndPosition,
					WorldRotation = Rotation.LookAt( tr.Normal )
				};

				var legacyParticleSystem = go.AddComponent<LegacyParticleSystem>();
				legacyParticleSystem.Particles = ParticleSystem.Load( particleName );
				legacyParticleSystem.ControlPoints = new()
				{
					new ParticleControlPoint { GameObjectValue = go, Value = ParticleControlPoint.ControlPointValueInput.GameObject }
				};

				go.NetworkSpawn( null );
				go.Network.SetOrphanedMode( NetworkOrphaned.Host );
				go.DestroyAsync( 5f );

				return legacyParticleSystem;
			}
		}

		return default;
	}


	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit by a bullet
	/// </summary>
	public static LegacyParticleSystem DoBulletImpact(this Surface self, SceneTraceResult tr)
	{
		return DoImpact( 
			self, 
			tr,
			( Surface.ImpactEffectData data ) => data.BulletDecal,
			( Surface.ImpactEffectData data ) => data.Bullet,
			( Surface.SoundData data ) => data.Bullet
		);
	}

	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit by a fist... close enough, at least
	/// </summary>
	public static LegacyParticleSystem DoFistImpact( this Surface self, SceneTraceResult tr )
	{
		return DoImpact(
			self,
			tr,
			( Surface.ImpactEffectData data ) => data.HardDecal,
			( Surface.ImpactEffectData data ) => data.HardParticles,
			( Surface.SoundData data ) => data.ImpactHard
		);
	}

	[Rpc.Broadcast]
	private static void BroadcastDoBulletImpact( string eventName, Vector3 position )
	{
		Sound.Play( eventName, position );
	}
}
