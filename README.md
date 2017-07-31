# Driver Tools [![Build status](https://ci.appveyor.com/api/projects/status/serhjoggv2bkrm56?svg=true)](https://ci.appveyor.com/project/Fireboyd78/driver-tools) #
Various modding tools for Driv3r, Driver: Parallel Lines, and Driver: San Francisco.

## Download ##
You may download the latest version of most programs [here](https://ci.appveyor.com/project/Fireboyd78/driver-tools/build/artifacts).

## Contributing ##
If you're interested in contributing, fork the repo and feel free to submit a pull request. Please keep code styling consistent. Thanks!

## Requirements ##
You'll need an IDE that supports C# 6.0 and atleast .NET Framework 4.0, unless otherwise specified. Most of the code here was written in Visual Studio 2015 Community.

## Licensing / Warranty ##
Everything is provided "as-is" and without _any_ warranty of _any_ kind. You may use and/or redistribute this code freely\*. No credit is necessary, but would be very much appreciated.

<sub>\*Any library dependencies are subject to their respective terms and agreements and/or copyright/licensing notices.</sub>

----

## Projects ##
A description of each project that can be found in this repository.

### Antilli ###
A 3D model viewer that utilizes WPF and supports loading Driv3r models. Includes a tool for editing chunk files.

### DSCript ###
The framework that holds everything together. Mostly contains code for working with game file formats. Name subject to change.

### GMC2Snooper ###
Experimental PS2 model loading tool. Once it's finished, the final changes will be merged into DSCript and Antilli.

### IMGRipper ###
Tool for working with PS2/XBox .IMG files used in games such as Stuntman, Driv3r, and Driver: Parallel Lines. Also includes rudimentary support for Driver '76 (PSP). This actually doesn't need to be in this repository, I just haven't gotten around to moving it.

### LocSF ###
Experimental tool for working with locale files in Driver: San Francisco. Doesn't do much at the moment.

### LuaSF ###
Allows for working with compiled script files in Driver: San Francisco. Requires my .NET port of a popular Lua 5.1 decompiler, [Unluac.NET](https://bitbucket.org/Fireboyd78/unluacnet).

### Zartex ###
Experimental tool for working with mission files in Driv3r. **ZartexV2** is unofficially the WPF port of the original WinForms version.
