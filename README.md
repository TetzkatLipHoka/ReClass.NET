# ReClass.NET
This is a port of ReClass to the .NET platform with lots of additional features.

![](https://abload.de/img/main4hsbj.jpg)

## Features
- Support for x86 / x64
- File import from ReClass 2007-2016 and ReClass QT
- Memory Nodes
  - Arrays and Pointers to every other node types
  - Hex 8 / 16 / 32 / 64
  - Int 8 / 16 / 32 / 64
  - UInt 8 / 16 / 32 / 64
  - Bool
  - Bits ![](https://abload.de/img/bitsnhlql.jpg)
  - Enumerations
  - Float / Double
  - Vector 2 / 3 / 4
  - Matrix 3x3 / 3x4 / 4x4
  - UTF8 / UTF16 / UTF32 Text and pointer to text
  - Virtual Tables
  - Function
  - Function Pointer
  - Unions
- Configurable Shortcuts for Nodes
- Automatic Node Dissection
- Highlight changed memory
- Pointer Preview
- Copy / Paste Support across ReClass.NET instances
- Display types from Debug Symbols (*.pdb)
- Display Runtime Type Informations (RTTI)
- Control the remote process: start / stop / kill
- Process Selection Dialog with filtering
- Memory Viewer
- Memory Scanner
  - Import files from Cheat Engine and CrySearch
  - Scan for values correlated to your input
- Class address calculator
- Code Generator (C++ / C#)
- Module / Section Dumper
- Linux Support (tested on Ubuntu 18.04)
- Debugger with "Find out what writes/accesses this address" support
- Plugin Support
  - Plugins can be written in different languages (example: C++, C++/CLI, C#)
  - Plugins can provide custom methods to access an other process (example: use a driver)
  - Plugins can interact with the ReClass.NET windows
  - Plugins can provide node infos which will be displayed (example: class informations for Frostbite games)
  - Plugins can implement custom nodes with load/save and code generation support
- DarkMode
- Configurable Editor Colors
  - Import/Export of Editor Themes
- Open MiniDumps
- Basic Networking-functionality
- Add arrow key selection in Class Selection window
- Show comments on Hex8Node
- Show comments on Hex16Node  
- HideWindowIcons
- Allow loading of uncompressed rcnet file to allow version control tools to work nicely w/ ReClass.NET files
- allow naming unions
- add support for generating C code
- Added **{ClassName}** support to the address field

## Plugins
- [Sample Plugins](https://github.com/ReClassNET/ReClass.NET-SamplePlugin)
- [Frostbite Plugin](https://github.com/ReClassNET/ReClass.NET-FrostbitePlugin)
- [MemoryPipe Plugin](https://github.com/ReClassNET/ReClass.NET-MemoryPipePlugin)
- [LoadBinary Plugin](https://github.com/ReClassNET/ReClass.NET-LoadBinaryPlugin)
- [Handle Abuser Plugin](https://github.com/ReClassNET/ReClass.NET-HandleAbuser)
- [Unreal Plugin](https://github.com/TetzkatLipHoka/ReClass.NET-UnrealPlugin) (by [DrP3pp3r](https://github.com/DrP3pp3r))
- [DriverReader](https://github.com/niemand-sec/ReClass.NET-DriverReader) (by [Niemand](https://github.com/niemand-sec))
- [PciLeech](https://github.com/mgoodings/ReClass.NET-PciLeechPlugin) (by [Dylan Goods](https://github.com/mgoodings/))
- [ReClass-DMA](https://github.com/dexus1337/ReClass-DMA) (by [dexus](https://github.com/dexus1337/))

- [Playstation 4: Frame4 Plugin](https://github.com/TetzkatLipHoka/ReClass.Net-Frame4Plugin)
- [Playstation 4: PS4Debug Plugin](https://github.com/TetzkatLipHoka/ReClass.Net-PS4DebugPlugin)



To install a plugin just copy it in the "Plugins" folder.
If you want to develop your own plugin just learn from the code of the [Sample Plugins](https://github.com/ReClassNET/ReClass.NET-SamplePlugin) and [Frostbite Plugin](https://github.com/ReClassNET/ReClass.NET-FrostbitePlugin) repositories. If you have developed a nice plugin, leave me a message and I will add it to the list above.

## Installation
Just download the [latest version](https://github.com/TetzkatLipHoka/ReClass.NET/releases) and start the x86 / x64 version or let the launcher decide.

## Tips
- Lots of elements have a context menu. Just right-click it and see what you can do there.
- The node window can be controlled with the keyboard too. Arrow keys can select other keys, combined with the shift key the nodes get selected. The menu key opens the context menu which itself can be controlled with the keyboard.
- The memory address field of a class can contain a real formula not just a fixed address.  
  
  **\<Program.exe> + 0x123** will use the base address of Program.exe and add 0x123 to it.
  **{ClassName}** will search for a class with this name in your project and resolve it's address.
  **[0x4012ABDE]** will read the integer (4 byte on x86 / 8 byte on x64) from the address 0x4012ABDE and use this value as class address.
  **[{ClassName} + 0x08]** will resolve the address of the ClassName in your project, add 0x08 and then read the value from this address (Useful to follow pointers from global objects) 
  **[\<Program.exe> + 0xDE] - AB** will use the base address of Program.exe, add 0xDE to it, read the value from this address and finally sub 0xAB from it.  
  **[\<Program.exe> + offset + [\<Program.exe> + offset2]]** Nested operations are supported too.  
  
  Valid operations are read ([..]), add (+), sub (-), mul (*) and div (/). Please note that all operations are integer calculations.

## Compiling
If you want to compile ReClass.NET just fork the repository and open the ReClass.NET.sln file with Visual Studio 2019.
Compile the project and copy the dependencies to the output folder.

To compile the linux native core library, you need WSL [installed and configured](https://learn.microsoft.com/en-us/cpp/build/walkthrough-build-debug-wsl2). If you do not need linux support, simply unload the project in the Solution Explorer. If you want to build cross-platform (x86/x64) you have to install `g++-multilib` too.

If you use the `Makefile` with `docker` or `podman` you have to build the needed image `gcc_multilib` from the following `Dockerfile` (`docker build -t gcc_multi .`):

```
FROM ubuntu:latest

RUN apt-get update \
 && apt-get install --assume-yes --no-install-recommends --quiet \
        make \
        g++ \
        g++-multilib \
 && apt-get clean all
```

## Videos

[Youtube Playlist](https://www.youtube.com/playlist?list=PLO246BmtoITanq3ygMCL8_w0eov4D8hjk)

## Screenshots

Process Selection

![image](https://github.com/user-attachments/assets/8a794152-1573-46c4-9c1f-8387977de614)


PS4

![image](https://github.com/user-attachments/assets/c8501516-482d-4264-9128-c5542183fcb8)


Memory Viewer  
![image](https://github.com/user-attachments/assets/d0ea7cc9-e89e-427f-965a-d57061531b5d)


Memory Scanner  
![image](https://github.com/user-attachments/assets/7805d290-1406-42a1-b87f-891508b2d0ed)


Pointer Preview  
![](https://abload.de/img/memorypreview2gsfp.jpg)

Code Generator  
![](https://abload.de/img/codegeneratorqdat2.jpg)
![](https://abload.de/img/codegenerator24qzce.jpg)

Plugins  
![image](https://github.com/user-attachments/assets/1f083968-01be-42a3-af6d-ef1a75926748)
![image](https://github.com/user-attachments/assets/39b32252-02a0-4c30-ae4c-e44cdefd7367)


Settings  
![image](https://github.com/user-attachments/assets/a4d0daca-6246-4f8f-8160-b51afbe29037)
![image](https://github.com/user-attachments/assets/26cc4b55-ebfd-421e-af7b-98d0203f2954)
![image](https://github.com/user-attachments/assets/0cd4ee64-e074-4865-ae31-753309e08611)
![image](https://github.com/user-attachments/assets/6da9d41d-ed2d-4bc6-a369-f4130844ddca)
![image](https://github.com/user-attachments/assets/89953f25-fea3-4061-a2b8-c2759d62ee6d)


## Authors / Special Thanks
- [KN4CK3R](https://github.com/KN4CK3R)
- DrUnKeN ChEeTaH
- P47R!CK
- DogMatt
- [ajkhoury](https://github.com/ajkhoury)
- [IChooseYou](https://github.com/IChooseYou)
- [stevemk14ebr](https://github.com/stevemk14ebr)
- [Timboy67678](https://github.com/Timboy67678)
- [DarthTon](https://github.com/DarthTon)
- [ReUnioN](https://github.com/ReUnioN)
- leveln
- [buddyfavors](https://github.com/buddyfavors)
- [DrP3pp3r](https://github.com/DrP3pp3r)
- [ko1N](https://github.com/ko1N)
- [Niemand](https://github.com/niemand-sec) (see his talk at [BlackHat Europe 2019 (London) "Unveiling the underground world of Anti-Cheats"](https://www.blackhat.com/eu-19/briefings/schedule/index.html#unveiling-the-underground-world-of-anti-cheats-17358))
- [Pharaoh2k](https://github.com/Pharaoh2k/)
- [Frans Bouma](https://github.com/FransBouma/)
- [João Vítor Moutinho Rocha](https://github.com/jvmr10/)
- [kapai](https://github.com/mkapai/)
- [sakiodre](https://github.com/sakiodre/)
- [кафіф](https://github.com/cafeed28/)
- [Gabriel-Marian Cristei](https://github.com/cristeigabriel/)
- [Peter Elmegaard](https://github.com/Elmegaard/)
- [Deniz Ozmus](https://github.com/dozmus/)
- [FynnTW](https://github.com/FynnTW/)
- [TechForBad](https://github.com/TechForBad/)
- [PeaceBeUponYou](https://github.com/PeaceBeUponYou/)
- [user23333](https://github.com/user23333/)
- [Pinwhell](https://github.com/pinwhell/)
- [Nu](https://github.com/Autoplay1999/)
- [Haselnussbomber](https://github.com/Haselnussbomber/)
- [atrexus](https://github.com/atrexus/)
- [dexus](https://github.com/dexus1337/)
- [dschu012](https://github.com/dschu012/)
- [Ross Weir](https://github.com/ross-weir/)
- [piepieonline](https://github.com/piepieonline/)