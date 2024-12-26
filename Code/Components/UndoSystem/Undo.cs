using System;
using Sandbox;

public class Undo
{
	public Player Creator;
	public GameObject Prop;
	public Func<string> UndoCallback;
	public float Time;

	public Undo( Player creator, GameObject prop, Func<string> callback )
	{
		Creator = creator;
		Prop = prop;
		UndoCallback = callback;
		Time = Sandbox.Time.Now;
	}
}
