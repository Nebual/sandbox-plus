# Custom Events provided by SandboxPlus

- "game.init": run on initial addon load
- "package.mounted": run when a package is async mounted via `spawnpackage`
- "sandbox.hud.loaded": run after the Sandbox.Hud has loaded, eg. for extending the spawnmenu
- "entity.spawned"
  - `Event.Run( "entity.spawned", IEntity spawned, IEntity owner )`
- "undo.add"
  - `Event.Run( "undo.add", Func<string> undo, IEntity owner )`
  - Add an "Undoable" lambda, which will be called if the player wants to undo.  
    Should return the string to show in the toast, or empty string if the undoable is redundant and should be skipped over (eg. if the weld was already removed)
- "spawnlists.initialize"
  - Takes no parameters; you're expected to call `ModelSelector.AddToSpawnlist( "screen", string[] models)`
- "player.cantool": called by traces.
  - Takes a single CanToolParams parameter. Writing `params.preventDefault = true` will prevent the tool action.
- "player.simulate"
  - `Event.Run( "player.simulate", SandboxPlayer player )`
- "player.killed"
  - `void OnKilled( SandboxPlayer player )`
- "trace.prepare": used to modify Player movement traces
  - ```
    [Event( "trace.prepare" )]
    public static void OnTracePrepare( Trace trace, Entity ent, Action<Trace> returnFn ) {
        returnFn(trace.WithoutTags( StargateTags.InBufferFront ) );
    }
    ```
- "weapon.shootbullet": used to allow overriding bullet behaviour, such as when shooting through a Stargate
  - Takes a single ShootBulletParams parameter. Writing `params.preventDefault = true` will disable the default bullet behaviour.

# Custom Concommands

- `weapon_switch physgun`
- `spawnpackage wiremod.sbox_tool_auto` - extended to work on runtime addons beyond just 'npc' + 'entity', see `Event("package.mounted")`
- `undo` + `redo`
- `reload_hud`, though it typically hotreloads nicely in the editor
- `tool_duplicator_savefile file.dupe` + `tool_duplicator_openfile file.dupe`

For more usage examples, see [Wirebox](https://github.com/wiremod/wirebox).
