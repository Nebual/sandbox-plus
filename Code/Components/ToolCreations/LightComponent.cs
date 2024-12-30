namespace Sandbox;

[Spawnable]
[Library( "ent_light", Title = "Light" )]
public partial class LightComponent : PointLight, Component.IPressable
{
	private bool _on = true;
	public bool On
	{
		get
		{
			return _on;
		}
		set
		{
			_on = value;
			if ( _on )
			{
				OnEnabled();
			}
			else
			{
				OnDisabled(); // this deletes the sceneObject of the Light
			}
		}
	}
	bool IPressable.CanPress( IPressable.Event e )
	{
		return true;
	}
	[Rpc.Broadcast]
	public void Press( GameObject presser )
	{
		if ( presser.Network.Owner != Rpc.Caller )
			return;

		On = !On;
		Sound.Play( On ? "flashlight-on" : "flashlight-off", WorldPosition );
	}

	bool IPressable.Press( IPressable.Event e )
	{
		Press( e.Source.GameObject );
		return true;
	}
}

public class LightWireComponent : BaseWireInputComponent
{
	public override void WireInitialize()
	{
		var light = GetComponent<LightComponent>();
		this.RegisterInputHandler( "On", ( bool value ) =>
		{
			light.On = value;
		} );
		this.RegisterInputHandler( "Color", ( Vector3 value ) =>
		{
			light.LightColor = value;
		} );
	}
}
