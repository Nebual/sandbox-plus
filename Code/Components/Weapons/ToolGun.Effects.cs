public partial class ToolGun
{
	static readonly string[] ShootSounds = [
		"airboat_gun_lastshot1",
		"airboat_gun_lastshot2",
	];
	protected LegacyParticleSystem beam;

	[Rpc.Broadcast]
	public void ToolEffects( Vector3 hitPos, Vector3 normal = new Vector3(), bool continuous = false )
	{
		var offset = normal * 0.66f; // A number of models have surfaces that differ from the physics hull, so let's add a small offset so the particle is always visible

		if ( continuous )
		{
			Particles.MakeParticleSystem( "particles/tool_hit.vpcf", new Transform( hitPos + offset ), 0.1f );
		}
		else
		{
			if ( normal.Length > 0 )
			{
				var random = new Random();
				var particle = Particles.MakeParticleSystem( "particles/tool_select_indicator.vpcf", new Transform( hitPos + offset ) );
				particle.SceneObject.SetControlPoint( 1, Rotation.LookAt( normal ) );
				particle.SceneObject.SetControlPoint( 2, new Vector3(
					// Actually a color. Blame Facepunch for calling it "SetPos".
					// These values are taken from Garry's Mod, and yet, they seem wrong...
					random.Next( 10, 150 ) / 255.0f,
					random.Next( 170, 220 ) / 255.0f,
					random.Next( 240, 255 ) / 255.0f
				) );
			}
			Particles.MakeParticleSystem( "particles/tool_hit.vpcf", Attachment( "muzzle" ) );

			beam = Particles.MakeParticleSystem( "particles/tool_tracer.vpcf", new Transform( hitPos ) );
			beam.SceneObject.SetControlPoint( 1, Attachment( "muzzle" ) );
			beam.SceneObject.SetControlPoint( 2, hitPos );

			if ( !IsProxy )
			{
				ViewModel?.Renderer?.Set( "fire", true );
			}

			int soundIndex = new Random().Next( 0, ShootSounds.Length );
			Sound.Play( ShootSounds[soundIndex], WorldPosition ).Volume = 0.5f;
		}
	}

	protected void UpdateEffects()
	{
		if ( beam.IsValid() && beam.SceneObject.IsValid() )
		{
			beam.SceneObject.SetControlPoint( 1, Attachment( "muzzle" ).Position + this.Owner.Controller.Velocity * Time.Delta );
		}
	}
}
