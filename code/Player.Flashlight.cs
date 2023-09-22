using Sandbox;

partial class SandboxPlayer
{
	private SpotLightEntity viewLight;
	private SpotLightEntity worldLight;
	private TimeSince timeSinceLightToggled;
	[Net, Predicted] private bool LightEnabled { get; set; }

	private Entity PreviousActiveChild;

	private void SetupFlashlight()
	{
		if ( Game.IsServer && !worldLight.IsValid() )
		{
			worldLight = CreateLight();
			worldLight.EnableHideInFirstPerson = true;
			PreviousActiveChild = null;
		}
		if ( Game.IsClient && !viewLight.IsValid() )
		{
			viewLight = CreateLight();
			viewLight.EnableViewmodelRendering = true;
			PreviousActiveChild = null;
		}

		if ( PreviousActiveChild != ActiveChild )
		{
			if ( ActiveChild.IsValid() && ActiveChild is BaseCarriable carriable )
			{
				if ( Game.IsServer )
				{
					if ( carriable.GetAttachment( "light" ) != null )
					{
						worldLight?.SetParent( carriable, "light", new Transform( Vector3.Forward * 10 ) );
					}
					else if ( carriable.GetAttachment( "muzzle" ) != null )
					{
						worldLight.SetParent( carriable, "muzzle", new Transform( Vector3.Forward * 25 ) );
					}
					PreviousActiveChild = ActiveChild;
				} else if ( Game.IsClient && carriable.ViewModelEntity.IsValid())
				{
					if ( carriable.ViewModelEntity.GetAttachment( "light" ) != null )
					{
						viewLight?.SetParent( carriable.ViewModelEntity, "light", new Transform( Vector3.Forward * 10 ) );
					}
					else if ( carriable.ViewModelEntity.GetAttachment( "muzzle" ) != null )
					{
						viewLight.SetParent( carriable.ViewModelEntity, "muzzle", new Transform( Vector3.Forward * 25 ) );
					}
					PreviousActiveChild = ActiveChild;
				}
			}
		}

	}

	private SpotLightEntity CreateLight()
	{
		var light = new SpotLightEntity
		{
			Enabled = LightEnabled,
			DynamicShadows = true,
			Range = 800,
			Falloff = 1.0f,
			LinearAttenuation = 0.0f,
			QuadraticAttenuation = 1.0f,
			Brightness = 1.5f,
			Color = Color.White,
			InnerConeAngle = 20,
			OuterConeAngle = 60,
			FogStrength = 1.0f,
			Owner = Owner,
			LightCookie = Texture.Load( "materials/effects/lightcookie.vtex" ),
		};

		return light;
	}
	
	internal void UpdateFlashlight( ) 
	{
		if ( ActiveChild is Flashlight )
		{
			return;
		}
		SetupFlashlight();
		if ( timeSinceLightToggled > 0.1f && Input.Pressed( "flashlight" ) )
		{
			LightEnabled = !LightEnabled;

			PlaySound( LightEnabled ? "flashlight-on" : "flashlight-off" );

			if ( worldLight.IsValid() )
			{
				worldLight.Enabled = LightEnabled;
			}

			if ( viewLight.IsValid() )
			{
				viewLight.Enabled = LightEnabled;
			}

			timeSinceLightToggled = 0;
		}
	}

}
