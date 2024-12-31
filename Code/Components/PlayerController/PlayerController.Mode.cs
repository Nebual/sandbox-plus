using SandboxPlus.Movement;
namespace SandboxPlus;


public sealed partial class PlayerController : Component
{
	public MoveMode Mode { get; private set; }

	void ChooseBestMoveMode()
	{
		var best = GetComponents<MoveMode>( false ).MaxBy( x => x.Score( this ) );
		if ( Mode == best ) return;

		Mode?.OnModeEnd( best );

		Mode = best;

		Body.PhysicsBody.Sleeping = false;

		Mode?.OnModeBegin();
	}
}
