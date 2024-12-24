using System;
using Sandbox;

public class Undo
{
	public Player Creator;
	public GameObject Prop;
	public Func<string> UndoCallback;
	public float Time;
	public bool Avoid;

	public Undo( Player creator )
	{
		Creator = creator;
		Time = Sandbox.Time.Now;
		Avoid = false;
	}
	public Undo( Player creator, GameObject prop ) : this( creator )
	{
		Prop = prop;
	}
}
