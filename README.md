# SandboxPlus, an S&box Game

Its built on top of [Sandbox Classic](https://github.com/Softsplit/sandbox) (which itself is a rewrite of Facepunch's Sandbox for the Scene system), with additions to make it more extendable (adding events, publicizing UI globals, etc), while staying unopinionated/light. The goal is "Gmod 2", replicating the functionality found in Gmod's Sandbox gamemode (with improvements).

For the old version using the Entity system, see [2023-entity-system](https://github.com/Nebual/sandbox-plus/tree/2023-entity-system) branch.

## Improvements over Facepunch/sandbox:

### Gameplay

- It exists (since Facepunch deleted theirs)
- Constraint tool: an omni-tool (like Precision Tool) with all your classic Gmod constraints (weld/axis/rope/elastic/slider/ballsocket) and nudge/rotate/move
- Undo System
- DynShapes spawnmenu: like PHX, but dynamic, fully customizable sizes using procedural meshes. Rectangles, cylinders, spheres, gears!
- Material tool
- Toolgun model
- UI tool panels, including ModelSelector with extendable `.spawnlist`'s
- [Wirebox](https://github.com/wiremod/wirebox) support

#### WIP (needs porting over from 2023-entity-system / CodeOld/)
- Duplicator tool
- [Many new events](EVENTS.md) (aiming to reproduce many of Gmod 1's), including "IPropSpawnedEvent" event, "undo.add" event

## Addons for SandboxPlus

- [Wirebox](https://github.com/wiremod/wirebox) - see its page for install instructions
- ~~[Stargate](https://github.com/Gmod4phun/sbox-stargate/tree/addon-version)~~ oudated
- ~~[Sbox Tool Auto](https://github.com/Nebual/sbox_tool_auto)~~ outdated, which is a simple example addon recreating gmod_tool_auto behaviour
- ~~[AdminEssentials](https://asset.party/ryan/adminessentials)~~ outdated, non-gamemode-specific, non-open-source.
- ~~[Napkins-Chat](https://github.com/Nebual/napkins-chat)~~ outdated

### Libraries

- [Wirelib](https://asset.party/wiremod/wirelib)
- ~~[NData (ClientRPC substitute)](https://github.com/Nebual/sbox-ndata)~~ outdated
- ~~[Permission framework](https://github.com/sandmod/permission)~~ outdated

## Contributing

PR's are appreciated!  
Message @Nebual on Discord with any questions :)

### Setup Guide

1. Clone the repo
2. Open the project in SBox Dev Tools
3. In the Asset Browser, right click Assets -> Recursively Recompile 300+ Assets

### Contributors

- [Nebual](https://github.com/Nebual)
- [Gmod4phun](https://github.com/gmod4phun)
- [thegrb93](https://github.com/thegrb93) - Duplicator tool
- [LtBrandon](https://github.com/LtBrandon) - Constraints, DynShapes
- [ninjrdevelop](https://github.com/ninjrdevelop)
- [Softsplit/sandbox](https://github.com/Softsplit/sandbox) - Base gamemode port of Facepunch/Sandbox into Scene system (Asphaltian, TROLLFACEINREALLIFE, badandbest, trende)
