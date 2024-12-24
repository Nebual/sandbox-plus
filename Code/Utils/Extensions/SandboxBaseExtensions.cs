/// <summary>
/// Extensions for Surfaces
/// </summary>
public static partial class SandboxBaseExtensions
{
	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit by a bullet
	/// </summary>
	public static LegacyParticleSystem DoBulletImpact( this Surface self, SceneTraceResult tr )
	{
		//
		// Drop a decal
		//
		var decalPath = Game.Random.FromList( self.ImpactEffects.BulletDecal );

		var surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( decalPath ) && surf != null )
		{
			decalPath = Game.Random.FromList( surf.ImpactEffects.BulletDecal );
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

		//
		// Make an impact sound
		//
		var sound = self.Sounds.Bullet;

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( sound ) && surf != null )
		{
			sound = surf.Sounds.Bullet;
			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( sound ) )
		{
			BroadcastDoBulletImpact( sound, tr.EndPosition );
		}

		//
		// Get us a particle effect
		//

		string particleName = Game.Random.FromList( self.ImpactEffects.Bullet );
		if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromList( self.ImpactEffects.Regular );

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( particleName ) && surf != null )
		{
			particleName = Game.Random.FromList( surf.ImpactEffects.Bullet );
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

		return default;
	}


	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit by a bullet
	/// </summary>
	public static LegacyParticleSystem DoFistImpact( this Surface self, SceneTraceResult tr )
	{
		var surf = self.GetBaseSurface();


		var decalPath = Game.Random.FromList( self.ImpactEffects.HardDecal );

		while ( string.IsNullOrWhiteSpace( decalPath ) && surf != null )
		{
			decalPath = Game.Random.FromList( surf.ImpactEffects.HardDecal );
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

		//
		// Make an impact sound
		//
		var sound = self.Sounds.ImpactHard;

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( sound ) && surf != null )
		{
			sound = surf.Sounds.ImpactHard;
			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( sound ) )
		{
			BroadcastDoBulletImpact( sound, tr.EndPosition );
		}

		//
		// Get us a particle effect
		//

		string particleName = Game.Random.FromList( self.ImpactEffects.HardParticles );
		if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromList( self.ImpactEffects.Regular );

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( particleName ) && surf != null )
		{
			particleName = Game.Random.FromList( surf.ImpactEffects.HardParticles );
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
				WorldRotation = Rotation.LookAt( tr.Normal ),
				LocalScale = new Vector3( 0.5f, 0.5f, 0.5f )
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

		return default;
	}

	[Rpc.Broadcast]
	private static void BroadcastDoBulletImpact( string eventName, Vector3 position )
	{
		Sound.Play( eventName, position );
	}
}
