# Wad-File-Emulator-ReloadedII

A [Reloaded II mod](https://github.com/Reloaded-Project/Reloaded-II) using the [File Emulation Framework](https://github.com/Sewer56/FileEmulationFramework) that emulates Danganronpa WAD files, allowing ReloadedII mods for the game that don't actually touch the physical wad files

You may need to install [File Emulation Framework](https://github.com/Sewer56/FileEmulationFramework) manually if it doesn't automatically get downloaded as a dependency when you install the mod in the mod folder / start your game. (Or you can just restart reloaded II and it should start to install required dependencies)

# How to make a mod that uses Wad File Emulator
Add "Wad File Emulator For File Emulation Framework" as a mod dependency

Create a folder in your mod folder named FEmulator

Create a folder within that folder named WAD

Then create a folder named after the wad file which content's you want to replace. eg: (dr1_data.wad, dr1_data_us.wad etc)

Then inside that folder paste the file you want to replace, following the internal stucture

eg: 
```py
YourMod/FEmulator/WAD/dr1_data.wad/Dr1/data/all/bgm/dr1_bgm_hca.awb.00001.ogg
```
