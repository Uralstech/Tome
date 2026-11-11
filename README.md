# Tome

Tome is a bare-bones mod manager for Deadlock, initially inspired by Kerbal Space Program's [CKAN](https://github.com/KSP-CKAN/CKAN).

Contributions are welcome! (although, heavily vibe-coded content is discouraged)

## Install Tome

Tome is published in two variants, both for x64 Windows. You can also use Tome on Linux, [as detailed by HasteReapr!](https://gamebanana.com/posts/14089687)

Both versions have the same functionality, but differ in dependency management and install size. You can download them from here, or the [Gamebanana](https://gamebanana.com/tools/24805) page:

<a href="https://github.com/Uralstech/Tome/releases/download/latestTome-Setup-Full-x64.exe"><img src="https://img.shields.io/badge/Full-Tome%3A_Windows_x64-black?style=for-the-badge&color=00ADEF"></a>

The "Full" installer includes the .NET 10 Desktop runtime, totalling Tome's installed size to ~160mb. Recommended if you do not plan on updating Tome frequently.

<a href="https://github.com/Uralstech/Tome/releases/download/latest/Tome-Setup-Slim-x64.exe"><img src="https://img.shields.io/badge/Slim-Tome%3A_Windows_x64-black?style=for-the-badge&color=00ADEF"></a>

The "Slim" installer uses a shared .NET 10 Desktop runtime that is installed on your system separate from Tome, making its installed size ~20mb.
Recommended if you already have the runtime installed and updated, or if you want to keep up to date on Tome's updates (download sizes will be reduced).

Both versions require Microsoft's WebView2 to render license text from mods, but most Windows 10 and all Windows 11 computers have it built-in.
Both installers will download WebView2 if not detected otherwise.

## TODO

- ~~Support mods that serve multiple archives which can both be installed (ref. [rabbit graves](https://gamebanana.com/mods/734072))~~ (added in v1.0.0-preview.2!)
- Maybe support performance config mods which override gameinfo.gi

## Find Mods

You can search mods by name or view the Featured and Top Submissions feeds from Gamebanana through their respective categories.
NSFW and content-warning flagged mods are filtered out by default. You can change the filters under `File -> Default Filters`:

- SFW Only (default): hides mods with content warnings or explicitly NSFW content.
- SFW + Content Warnings: includes warning-labeled mods.
- All Content: also includes explicitly NSFW mods.

Search queries must be at least three characters long. Tome also accepts the following advanced filters in search queries (do not include square brackets in the actual query):

- `$sort:[new | default | updated]`: sort mods with specified priority
- `$show:[all | warn | default]`: show all mods including NSFW content (`all`), show SFW mods and mods with content warnings (`warn`), use the currently set global filter (`default`)
- `$types:[Mod | Sound]`: comma separated list of mods to show by type (currently only `Mod` and `Sound` submissions)
- `$-types:[Mod | Sound]`: comma separated list of mods to *exclude* by type
- `$typeId:[Mod | Sound],[SubmissionId]`: search for specific mod, by type and unique ID
- `$link:[GamebananaLink]`: search for specific mod by Gamebanana link

You can select the `?` button on a listed mod to view more information about it before installing (like licenses).

## Install, Update, and Delete Mods

Just like CKAN, you can click on a listed mod's `Install` checkbox to enqueue it for installation.
The same applies to the `Update` checkbox, which will only be interactable if an update is available.

You can enqueue an already installed mod for deletion by unchecking `Install`.
All installed and enqueued mods can be viewed under the `Installed & Pending` category.

Tome installs VPK (Valve Pack) files. It can also process ZIP, RAR, TAR, GZip, and 7Z archives that
contain exactly one VPK file. Non-VPK files in the archive are ignored.

You can install local mods from your computer from `File -> Add Local Mod`. Select a VPK or supported
archive, give it a name and (optional) author. You can optionally choose to replace/update an existing mod
with the local mod.

Local mods are not checked for conflicts.

## Build Tome

My development environment is Visual Studio with the .NET (10) desktop development module.

You can build Tome with:
```cmd
dotnet build Tome\Tome.csproj
```

Note that the built version of Tome will not work with Gamebanana's one-click install,
as that is handled by the installer. You will have to manually edit Windows Registry keys
to link your built version of Tome with the `tome:` URL protocol.

## Credits

- [s0nought/gb-api-v11](https://www.postman.com/s0nought/gb-api-v11/overview): Gamebanana API reference.
- [ValveKeyValue](https://github.com/ValveResourceFormat/ValveKeyValue): for parsing gameinfo.gi
- [steamlocate-rs](https://github.com/WilliamVenner/steamlocate-rs/): reference for locating the user's Steam installation
- [InnoDependencyInstaller](https://github.com/DomGries/InnoDependencyInstaller): .NET & WebView installation for installers
- [Inno-Setup-Action](https://github.com/Minionguyjpro/Inno-Setup-Action): Inno Setup for GitHub actions
- [Deadlock Mod Manager](https://github.com/deadlock-mod-manager/deadlock-mod-manager) and [Grimoire Mod Manager](https://github.com/Slush97/grimoire): reference on how Deadlock patching works