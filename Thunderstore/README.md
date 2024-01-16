# Description

### Simple mod that allows First Person. It moves the camera into the player's head. It "hides" what is needed either through culling the character model or hiding the head by shrinking it (configuration value).

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly, upon file save, it will update the settings.`


---
<details><summary><b>Configuration Options</b></summary>


`1 - General`

Lock Configuration [Synced with Server]

* If on, the configuration is locked and can be changed by server admins only. All Synced With Server configurations
  will be enforced to the clients.
    * Default Value: On

`1 - Toggles`

Enable First Person [Synced with Server]

* If on, First Person is enabled.
    * Default Value: On

Enforce First Person [Synced with Server]

* If on, First Person is enforced to always be on. Respects the Enable First Person configuration and both must be on
  for First Person to be enforced.
    * Default Value: Off

Hide Head [Not Synced with Server]

* If on, the camera will not use the culling mode and will instead shrink the head to hide it. This method is a bit
  better overall as your armor isn't see through, but looks a little weird. Headless people always do.
    * Default Value: Off

`2 - Camera`

Default FOV [Not Synced with Server]

* Default FOV for First Person.
    * Default Value: 65

NearClipPlaneMin [Not Synced with Server]

* Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility;
  too high might clip nearby objects.
    * Default Value: 0.17

NearClipPlaneMax [Not Synced with Server]

* Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility;
  too high might clip nearby objects.
    * Default Value: 0.17

`3 - Keyboard Shortcuts`

Toggle First Person Shortcut [Not Synced with Server]

* Keyboard Shortcut needed to toggle First Person. If FirstPersonMode is enforced, you cannot toggle.
    * Default Value: H + LeftShift

Raise FOV Shortcut [Not Synced with Server]

* Keyboard Shortcut needed to raise FOV.
    * Default Value: PageUp + LeftShift

Lower FOV Shortcut [Not Synced with Server]

* Keyboard Shortcut needed to lower FOV.
    * Default Value: PageDown + LeftShift

</details>

<details><summary><b>Installation Instructions</b></summary>

***You must have BepInEx installed correctly! I can not stress this enough.***

### Manual Installation

`Note: (Manual installation is likely how you have to do this on a server, make sure BepInEx is installed on the server correctly)`

1. **Download the latest release of BepInEx.**
2. **Extract the contents of the zip file to your game's root folder.**
3. **Download the latest release of FirstPersonMode from Thunderstore.io.**
4. **Extract the contents of the zip file to the `BepInEx/plugins` folder.**
5. **Launch the game.**

### Installation through r2modman or Thunderstore Mod Manager

1. **Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
   or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).**

   > For r2modman, you can also install it through the Thunderstore site.
   ![](https://i.imgur.com/s4X4rEs.png "r2modman Download")

   > For Thunderstore Mod Manager, you can also install it through the Overwolf app store
   ![](https://i.imgur.com/HQLZFp4.png "Thunderstore Mod Manager Download")
2. **Open the Mod Manager and search for "FirstPersonMode" under the Online
   tab. `Note: You can also search for "Azumatt" to find all my mods.`**

   `The image below shows VikingShip as an example, but it was easier to reuse the image.`

   ![](https://i.imgur.com/5CR5XKu.png)

3. **Click the Download button to install the mod.**
4. **Launch the game.**

</details>

<br>
<br>

`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>

<details><summary>
V+ Developer Credits (For some of the code used in this mod)

</summary>

# ValheimPlus Official Development Team [![ValheimPlus Icon](https://raw.githubusercontent.com/nxPublic/ValheimPlus/master/ico.png)](https://discord.valheim.plus)

* Kevin 'nx#8830' J.- https://github.com/nxPublic
* Miguel 'Mixone' T. - https://github.com/Mixone-FinallyHere
* Lilian 'healiha' C. - https://github.com/healiha
* Nathan 'NCJ' J. - https://github.com/ncjsvr

# Credits

* Greg 'Zedle' G. - https://github.com/zedle
* Paige 'radmint' N. - https://github.com/radmint
* Chris 'Xenofell' S. - https://github.com/cstamford
* TheTerrasque - https://github.com/TheTerrasque
* Bruno Vasconcelos - https://github.com/Drakeny
* GaelicGamer - https://github.com/GaelicGamer
* Doudou 'xiaodoudou' - https://github.com/xiaodoudou
* MrPurple6411#0415 - BepInEx Valheim version, AssemblyPublicizer
* Mehdi 'AccretionCD' E. - https://github.com/AccretionCD
* Zogniton - https://github.com/Zogniton - Inventory Overhaul initial creator
* Jules - https://github.com/sirskunkalot
* Lilian Cahuzac - https://github.com/healiha
* Thomas 'Aeluwas#2855' B. - https://github.com/exscape
* Nick 'baconparticles' P. - https://github.com/baconparticles
* An 'Hachidan' N. - https://github.com/ahnguyen09
* Abra - https://github.com/Abrackadabra
* Increddibelly - https://github.com/increddibelly
* Radvo - https://github.com/Radvo

</details>