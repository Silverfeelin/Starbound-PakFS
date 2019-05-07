# Starbound PakFS

Experimental ProjFS provider for Starbound pak files. Based on [ProjFS-Managed-API](https://github.com/Microsoft/ProjFS-Managed-API).

With the asset unpacker, it can take minutes to unpack files before you can browse through the assets and open the files you're looking for.

| ![](https://i.imgur.com/NHPPHQb.png) |
|---|

With the provider, the pak file is projected as a directory without needing to unpack it first. Files that you open or copy

> Note: PakFS is not meant as a replacement tool for unpacking files. As it integrats with the Windows Explorer default behaviour, it could very well end up being slower when copying a large amount of files.

## Prerequistes

* Windows Projected File System
* [.NET Framework 4.6.1](https://www.microsoft.com/en-US/download/details.aspx?id=49981) or higher.

To enabled the Windows Projected File System, launch `Windows Features` from the start menu and enable it from the list, as illustrated in the image below.

![](https://i.imgur.com/UXcLoGe.png)

## Installation

* Unpack the release somewhere on your computer. It doesn't matter where, as long as you can locate it in the next steps.
* Go to a pak file, right click and select <kbd>Open with...</kbd> from the context menu.
* Locate the `Starbound-PakFS.exe` file and select it.
  * Optionally, set this as your default application for pak files.

## Usage

#### Opening

Assuming you set `Starbound-PakFS.exe` as the default application for opening pak files, using the tool is as simple as double clicking on any pak file.  
This will project the file into a new folder `_fileName_pak`. A console will open and the file will projected as long as this console remains open.

![](https://i.imgur.com/ycbCHBx.png)

#### Navigating

Navigating the projected file is the same as navigating any folder in Windows (_that's the entire point of this application_).

> Files you open or copy will be written to your disk. This means that any files you open will be unpacked. Files you don't touch on the other hand will remain projected until the application closes.

![](https://i.imgur.com/BCPndu0.png)

#### Stoppping

When you want to stop projecting the file, go to the console and hit any key.  
If you want to fully delete the projected folder, close the console with the <kbd>X</kbd> key.

## Credits

* The developers behind the [ProjFS-Managed-API](https://github.com/Microsoft/ProjFS-Managed-API) project for allowing us to write providers in managed code (_yay C#_).
* Kawa for C# code to read and parse pak files. You can find more of Kawa's works on their website [The Helmeted Rodent](https://helmet.kafuka.org).
* Yusuke Kamiyamane for the application icon, part of the [Fugue Icons pack](https://p.yusukekamiyamane.com/) licensed under a [CC Attribution 3.0 license](https://creativecommons.org/licenses/by/3.0/).
