Development on hold until I decide to pick it up again. :)
Should work as is, leave an issue if not. Glhf!

# Unispect
![unispect](https://github.com/Razchek/Unispect/blob/master/Gallery/UnispectLogo.png?raw=true)
 
Unispect is a mono type definition and field inspector targeting Unity games compiled with mono.
It does so by accessing the remote process's memory.

![Screenshot0](https://github.com/Razchek/Unispect/blob/master/Gallery/poweredByCoffee.png?raw=true)

The design choice of accessing the process memory to gather the definitions was made with the intention 
of being able to access the run-time type definitions as well as accurate field definition information.

![Screenshot1](https://github.com/Razchek/Unispect/blob/master/Gallery/screenshot1.png?raw=true)

# Download
Head over to the [releases](https://github.com/Razchek/Unispect/releases) page.

# Features

  - Display type definitions from classes, structures, interfaces and enums
  - Display field definitions including offsets, types and static values *¹
  - Automatic deobfuscation of obfuscated names *²
  - Save definitions to a formatted document for manual digestion
  - Save definitions into a shareable database which can then be loaded back into Unispect anytime
  - Plugin interface for custom memory access implementations
  - Track types in more detail in the inspector by simply clicking on the base or extended type
  - Fast definition collection. Only a few seconds even when using the native memory implementation.
  - Drag & drop definitions from the Type Definition Inspector into apps like Notepad++, Chrome, etc *³
  
 *¹ _Static values are still being implemented_
 
 *² _The deobfuscated names should match between users. I need collaborative results to validate this._
 
 *³ _The standard notepad only accepts files, I did not want to create a temporary file (or otherwise) to accomodate for that._

### Drag & Drop
From the Type Inspector view (see below) simply click and drag a Type Definition or Field Definition into your desired application's text input box.
That action will produce either:

```css
Type Definition output:
[Class] BehaviourMachine.ObjectRandom : ActionNode
    [00][S] onNodeTick : System.Action<ActionNode>
    [10] m_Status : System.Int32
    [18] m_Branch : BehaviourMachine.BranchNode
    [20] m_Self : UnityEngine.GameObject
    [28] m_Owner : BehaviourMachine.INodeOwner
    [30] instanceID : Int32
    [38] name : String
    [40] objects : BehaviourMachine.ObjectVar[]
    [48] storeObject : BehaviourMachine.ObjectVar

Field Definition output:
BehaviourMachine.ObjectRandom->m_Branch // Offset: 0x0018 (Type: BehaviourMachine.BranchNode)
```

![Screenshot2](https://github.com/Razchek/Unispect/blob/master/Gallery/screenshot2.png?raw=true)

Planned features (these aren't definite, but likely):
  - The ability to drag desired type hierarchies into a project view
  - Save project state so you can review the information at a later time
  - Export a .NET Framework dynamic link library using the project information
  - Changes to the application interface, more UI elements to make swift browsing more accessible
 
# Current Limitations & Thoughts
  - Currently only tested on Unity 2018 and Unity 2019 builds. When I push the Assembly Export feature, I will also convert the static structures used to read all of the remote information into dynamic structures and allow the offsets to be customized with a JSON file. This will allow Unispect to target a broader spectrum of Unity versions.
  - Currently only games using mono bleeding edge (mono-2.0-bdwgc) are supported. Standard mono is in the scope of this project and will be looked at in the near future.
  - Only works with Unity Scripting Backend: Mono. IL2CPP may be supported in the future.
  - Only works with x64 systems and software. I might (unlikely) add support for x32 in the future.
  - Static, constant and enum values are not shown. Still figuring those out.
  - Method definitions are not collected. This is intentional, but I may implement it in the future with good reason.
  - You can only view a type definition from another MonoImage if it's the parent of one from the current MonoImage.
  
    *(I will probably change the code to iterate over all modules and collect all information in the future)*
  - Unispect's 'Deobfuscation' only targets the most common obfuscation seen on Unity's managed assemblies.  

Check out the [issues tab](https://github.com/Razchek/Unispect/issues) for more.
 
# Tech

Unispect uses these projects (or plans to):

* [MahApps.Metro] - A toolkit for creating modern WPF applications. Lots of goodness out-of-the box.
* [Fody.Costura] - Merges IL assemblies together, makes things more portable!
* [DynamicStructs] (Currently Private) - A dynamic struct generator written by me. :)
  
# Installation

Unispect requires the [.NET Framework v4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) or higher to be installed in order to run.
I currently have no plans on porting it to other frameworks or platforms.

# Example file output (small snippet):
```css
[Class] GPUInstancer.SpaceshipMobileController : MonoBehaviour
    [00][S] OffsetOfInstanceIDInCPlusPlusObject : Int32     // Static fields are marked with [S]
    [00][C] objectIsNullMessage : String                    // Constant fields are marked with [C]
    [00][C] cloneDestroyedMessage : String
    [10] m_CachedPtr : IntPtr
    [18] spaceShipJoystick : GPUInstancer.SpaceshipMobileJoystick
    [20] rigidbody_0x20 : UnityEngine.Rigidbody             // Deobfuscated fields are named like this
    [28] emissionModule_0x28 : -.ParticleSystem.EmissionModule
    [30] emissionModule_0x30 : -.ParticleSystem.EmissionModule
    [38] light_0x38 : UnityEngine.Light
    [40] engineTorque : Single
    [44] enginePower : Single
    [48] single_0x48 : Single
    [4C] single_0x4C : Single
    [50] single_0x50 : Single
    [54] single_0x54 : Single
    [58] single_0x58 : Single
    [5C] single_0x5C : Single
[Class] GPUInstancer.SpaceshipMobileJoystick : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IDragHandler
    [00][S] OffsetOfInstanceIDInCPlusPlusObject : Int32
    [00][C] objectIsNullMessage : String
    [00][C] cloneDestroyedMessage : String
    [10] m_CachedPtr : IntPtr
    [18] image_0x18 : UnityEngine.UI.Image
    [20] image_0x20 : UnityEngine.UI.Image
    [28] inputDirection : UnityEngine.Vector3
    [34] vector2_0x34 : UnityEngine.Vector2
```

# Plugins

  - Start a new Class Library (.NET Framework) project and replace your starting code with the code from [MemoryPluginTemplate.cs]
  - Add a reference to Unispect.exe (Project > Add Reference... > Browse)
  - Change your Build Platform target to x64
  - Edit the code to your hearts content
  - Compile the class library and place the .dll into Unispect's plugins folder (Unispect\Plugins\)
  - Open Unispect and click "Load Plugin". Look for your class name and then select it by clicking on it.
  
![Screenshot1](https://github.com/Razchek/Unispect/blob/master/Gallery/screenshot3.png?raw=true)


# Build
   - **Visual Studio 2019:** *Build > Build Solution (F6)*
   - **Visual Studio 2017:** Remove the 'Fody.Costura' NuGet package then *Build > Build Solution (F6)*

# Support
Contribute? Nice! Fork and request a pull.
Or feel free to [buy me a coffee!](ko-fi.com/razchek)

Before requesting a pull, be sure to test the target changes.
If the testing fails, feel free to create an [issue](https://github.com/Razchek/Unispect/issues) instead.

# License
----
All code, NuGets and binaries are under the [MIT License (MIT)]

   [MahApps.Metro]: <https://github.com/MahApps/MahApps.Metro>
   [Fody.Costura]: <https://github.com/Fody/Costura>
   [MIT License (MIT)]: <https://github.com/Razchek/Unispect/blob/master/LICENSE>
   [MemoryPluginTemplate.cs]: <https://github.com/Razchek/Unispect/blob/master/Unispect/Plugins/MemoryPluginTemplate.cs>
