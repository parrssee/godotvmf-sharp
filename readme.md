<p align="center">
<img src="https://github.com/user-attachments/assets/e1959708-fc5a-4245-aed4-5b7f3044aada" width="10%" />
</p>

<h2 align="center"> GodotVMF (C# port) </h2>

<p align="center">

## Original author's resources:
<a href="https://discord.gg/wtSK94fPxd" target="_blank">
<img src="https://img.shields.io/badge/Get%20Support%20in%20Discord-%235865F2?style=for-the-badge&logo=discord&logoColor=white&text=get-support" alt="Discord"></a>

<a href="https://godotengine.org/asset-library/asset/2605" target="_blank">
<img src="https://img.shields.io/badge/asset_library-%23EEEEEE.svg?style=for-the-badge&logo=godot-engine" alt="Godot Asset Library"></a>

<a href="https://store-beta.godotengine.org/asset/h2xdev/godotvmf" target="_blank">
<img src="https://img.shields.io/badge/asset_store-%23333333.svg?style=for-the-badge&logo=godot-engine&logoColor=%23ffffff" alt="Godot Asset Store"></a>
</p>

## What is this?
This is a full C# rewrite of [H2xDev/GodotVMF](https://github.com/H2xDev/GodotVMF), an importer of [VMF files](https://developer.valvesoftware.com/wiki/VMF_(Valve_Map_Format)) into [Godot Engine](https://godotengine.org/). Every GDScript file in the original addon, its plugin/importer core, and the entity scripts have been ported line-for-line to C#, so the whole plugin can be used from a C#-only Godot project without a GDScript dependency.

On top of the base addon, this fork also bundles the entity catalog that upstream ships separately:
- All gameplay entities from [H2xDev/GodotVMF-Entities](https://github.com/H2xDev/GodotVMF-Entities)
- The demo-project entities from [H2xDev/GodotVMF-Project-Template](https://github.com/H2xDev/GodotVMF-Project-Template) (`func_door`, `func_button`, `func_door_rotating`, `func_tracktrain`, `ambient_generic`, `env_fade`, `env_shake`, `env_fog_controller`, `game_text`, `info_particle_system`, `info_player_start`, `path_track`, `point_teleport`, `point_viewcontrol`, `func_brush`, ...)

so the addon works with a Hammer-built map out of the box, instead of requiring the entities to be copied in from two extra repos.

Highly recommended to use [Hammer++](https://ficool2.github.io/HammerPlusPlus-Website/) since it supports precised vertex data.

### Features
- Brushes geometry import (including UVs, materials IDs and smoothing groups)
- Instances support
- Native MDL support
- Native VMT support
- Native VTF support (only DXT1, DXT3, DXT5 supported)
- Displacements import (with vertex data)
	- WorldVertexTransition materials (blend textures) will be imported as [`WorldVertexTransitionMaterial`](/addons/godotvmf/shaders/WorldVertexTransitionMaterial.gd)
- Entities support (full catalog bundled, see above)
- Hammer's Input/Output system support
- Surface props support
- Material's compile properties support

<img src="https://github.com/user-attachments/assets/21084c3e-3530-45e5-8e05-d669d2a3ecf1" width="100%" />

## Why a C# port?
The original GodotVMF is GDScript-first, which is a great fit for pure-GDScript projects but gets awkward once a project's gameplay code lives in C#: every entity hook ends up going through `Call()`/dynamic dispatch across the language boundary. This fork exists to remove that boundary entirely - the importer, the entity base classes and every shipped entity are plain C#, so a C# gameplay project can subclass, extend, and debug them like any other project script.

The behavior is intended to match upstream as closely as possible; divergences are bug fixes found while porting rather than deliberate redesigns.

## Versioning
Version strings follow `<upstream-version>+cs.<port-revision>` (SemVer build metadata), e.g. `2.2.11+cs.1`:
- The three-part base always equals the exact upstream [GodotVMF](https://github.com/H2xDev/GodotVMF) tag this fork was last synced to.
- `+cs.N` increments for each port-only release (bugfixes, entity fixes, etc.) cut between upstream syncs.
- On the next upstream resync, the base is bumped to the new upstream version and the suffix resets to `+cs.1`.

```
2.2.11+cs.1   <- initial C# port of upstream v2.2.11
2.2.11+cs.2   <- port-only bugfix, still baseline v2.2.11
2.2.12+cs.1   <- resynced to upstream v2.2.12, port revision resets
```

Git tags mirror the version string with a `v` prefix (e.g. `v2.2.11+cs.1`). The current version is tracked in [`addons/godotvmf/plugin.cfg`](addons/godotvmf/plugin.cfg) and can be bumped via the "Version Change" GitHub Actions workflow.

## Made with the original tool
- [Echo Point](https://www.youtube.com/watch?v=z7LcKb0XRzY) by Lazy
- [Vampire Bloodlines map example](https://www.youtube.com/watch?v=dV3nllCZYNM)  by Rendara
- [SurfsUp](https://store.steampowered.com/app/3454830/SurfsUp) by [@bearlikelion](https://github.com/bearlikelion)
- [Team Fortress Jumper](https://github.com/Mickeon/team-fortress-jumper) by [Mickeon](https://github.com/Mickeon)

## Installation and Usage
Installation and map-side workflow is unchanged from upstream:
- [Installation Guide](https://github.com/H2xDev/GodotVMF/wiki/Installation-guide)
- [Installation Guide Video](https://www.youtube.com/watch?v=QqeAfOaABUI)
- [Documentation](https://github.com/H2xDev/GodotVMF/wiki)
- [Materials Video Tutorial](https://www.youtube.com/watch?v=6anSX-sWgW0)

The only difference: since every upstream entity is already bundled and ported to C#, there's no separate `GodotVMF-Entities` or `GodotVMF-Project-Template` install step - just install this addon.

## Known issues
- Extraction of materials and models from VPKs is not supported
- Some of imported models may have wrong orientation
    - Use `Additional Rotation` property in the MDL import options
- Avoid importing a big bunch of models/materials at once it may cause the engine crash or import freeze. There's some issue with threaded import in the engine.
- The FGD generator from upstream (compiling a FGD from entity source, see [here](https://github.com/H2xDev/GodotVMF/wiki/FGD-Generation)) has not been ported yet, since it depended on parsing GDScript source directly.

## Legality of use
If you would like to use the Source Engine SDK or other Valve Developer Tools for commercial use, please contact Valve at sourceengine@valvesoftware.com. There shouldn’t be any issues if you’re using it for non-commercial projects.  

## Contribution
If you have some ideas, suggestions regarding to quality or solutions of the problems above, feel free to contribute!
- If you've added a new feature please add the relevant documentation.
- Add yourself to the contributors section below

### How to test the addon after adding new features or fixing some bugs
1. Install any of Source Engine Games (L4D, HL2, TF2)
2. Unpack all textures and models from VPKs
3. Decompile most complex maps
4. Try to import decompiled maps in Godot
5. Check for errors if they appear

## Credits
[H2xDev](https://github.com/H2xDev) - original GodotVMF author and maintainer   
[Ambiabstract](https://github.com/Ambiabstract) - tech help and inspiration  
[Lachrymogenic](https://github.com/Lachrymogenic) - linux test, performance test  
[SharkPetro](https://github.com/SharkPetro) - materials test  
[parrssee](https://github.com/parrssee) - C# port

### Contributors
[Mickeon](https://github.com/Mickeon)
[URAKOLOUY5](https://github.com/URAKOLOUY5)
[ckaiser](https://github.com/ckaiser)
[jamop4](https://github.com/jamop4)
[Catperson6](https://github.com/catperson6real-dev)

## License
MIT, same as upstream.
