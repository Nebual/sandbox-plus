namespace SandboxPlus;

public sealed partial class PlayerController : Component
{
	/// <summary>
	/// Make sure the body and our components are created
	/// </summary>
	void EnsureComponentsCreated()
	{
		if ( !ColliderObject.IsValid() )
		{
			ColliderObject = GameObject.Children.FirstOrDefault( x => x.Name == "Colliders" );
			if ( !ColliderObject.IsValid() )
			{
				ColliderObject = new GameObject( true, "Colliders" );
				ColliderObject.Parent = GameObject;
			}
		}

		ColliderObject.LocalTransform = global::Transform.Zero;
		ColliderObject.Tags.SetFrom( BodyCollisionTags );

		Body.CollisionEventsEnabled = true;
		Body.CollisionUpdateEventsEnabled = true;
		Body.RigidbodyFlags = RigidbodyFlags.DisableCollisionSounds;

		BodyCollider = ColliderObject.GetOrAddComponent<CapsuleCollider>();
		FeetCollider = ColliderObject.GetOrAddComponent<BoxCollider>();

		Body.Flags = Body.Flags.WithFlag( ComponentFlags.Hidden, !_showRigidBodyComponent );

		ColliderObject.Flags = ColliderObject.Flags.WithFlag( GameObjectFlags.Hidden, !_showColliderComponent );

		if ( Renderer is null && UseAnimatorControls )
		{
			Renderer = GetComponentInChildren<SkinnedModelRenderer>();
		}
	}

	/// <summary>
	/// Update the body dimensions, and change the physical properties based on the current state
	/// </summary>
	void UpdateBody()
	{
		var feetHeight = BodyHeight * 0.5f;
		var radius = (BodyRadius * MathF.Sqrt( 2 )) / 2;

		BodyCollider.Radius = radius;
		BodyCollider.Start = Vector3.Up * (BodyHeight - BodyCollider.Radius);
		BodyCollider.End = Vector3.Up * (BodyCollider.Radius + feetHeight - BodyCollider.Radius * 0.20f);
		BodyCollider.Friction = 0.0f;
		BodyCollider.Enabled = true;

		FeetCollider.Scale = new Vector3( BodyRadius, BodyRadius, feetHeight );
		FeetCollider.Center = new Vector3( 0, 0, feetHeight * 0.5f );
		FeetCollider.Friction = IsOnGround ? 10f : 0;
		FeetCollider.Enabled = true;

		var locking = Body.Locking;
		locking.Pitch = true;
		locking.Yaw = true;
		locking.Roll = true;
		Body.Locking = locking;

		Body.MassOverride = BodyMass;

		// Move the center of mass to the 
		Body.OverrideMassCenter = true;

		float massCenter = IsOnGround ? WishVelocity.Length.Clamp( 0, BodyHeight * 0.5f ) : BodyHeight * 0.5f;
		Body.MassCenterOverride = Body.MassCenterOverride.LerpTo( new Vector3( 0, 0, massCenter ), Time.Delta * 10 );

		Mode?.UpdateRigidBody( Body );
	}
}
