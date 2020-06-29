Not currently ready for github. Getting there, please be patient. :)

------

# Unispect

![Screenshot0](https://github.com/Razchek/Unispect/blob/master/Screenshots/poweredByCoffee.png?raw=true)
 
Unispect is a mono type definition and field inspector targetting Unity games compiled with mono.
It does so by accessing the remote process's memory.

The design choice of accessing the process memory to gather the definitions was made with the intention 
of being able to access the run-time type definitions as well as accurate field definition information.


![Screenshot1](https://github.com/Razchek/Unispect/blob/master/Screenshots/screenshot1.png?raw=true)

# Features

  - Display type definitions from classes, structures, interfaces and enums
  - Display field definitions including offsets, types and static values *¹
  - Automatic deobfuscation of obfuscated names *²
  - Save processed information to a formatted document for manual digestion
  - Plugin interface for custom memory access implementations
  - Track types in more detail in the inspector by simply clicking on the base or extended type
 
 *¹_Static values are still being implemented_
 *²_These are hashed and will match between users, but they will not match de4dot's naming_

![Screenshot2](https://github.com/Razchek/Unispect/blob/master/Screenshots/screenshot2.png?raw=true)

Planned features:
  - The ability to drag desired type hierarchies into a project view
  - Save project state so you can review the information at a later time
  - Export a .NET Framework dynamic link library using the project information
  - Changes to the application interface, more UI elements to make swift browsing more accessible
 
### Tech

Unispect uses these projects:

* [MahApps.Metro] - A toolkit for creating modern WPF applications. Lots of goodness out-of-the box.
* [DynamicStructs] (Currently Private) - A dynamic struct generator written by me. :)
  
### Installation

Unispect requires the [.NET Framework v4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) or higher to be installed in order to run.
I currently have no plans on porting it to other frameworks or platforms.

 ### Example file output (small snippet):
```css
[Class] GPUInstancer.SpaceshipMobileJoystick : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IDragHandler
    [00] OffsetOfInstanceIDInCPlusPlusObject : Int32
    [00] objectIsNullMessage : String
    [00] cloneDestroyedMessage : String
    [10] m_CachedPtr : IntPtr
    [18] image_0x18 : UnityEngine.UI.Image
    [20] image_0x20 : UnityEngine.UI.Image
    [28] inputDirection : UnityEngine.Vector3
    [34] vector2_0x34 : UnityEngine.Vector2
[Class] GPUInstancer.TerrainGenerator : MonoBehaviour
    [00] HELPTEXT_detailHealthyColor : String
    [00] OffsetOfInstanceIDInCPlusPlusObject : Int32
    [00] objectIsNullMessage : String
    [00] cloneDestroyedMessage : String
    [08] HELPTEXT_detailDryColor : String
```

### Plugins

Information to come.


### Support
Contribute? Nice! Fork and request a pull.
 

License
----
Currently all copyrights are reserved. This is subject to change.

   [MahApps.Metro]: <https://github.com/MahApps/MahApps.Metro>
