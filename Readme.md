# TConvert ![AppIcon](http://i.imgur.com/5WPwZ3W.png)
A combination tool for managing Terraria content resources. Convert, extract, backup, and restore. The unofficial sequal to TExtract.

![Window Preview](http://i.imgur.com/oTuVrGQ.png)

### [Wiki](https://github.com/trigger-death/TConvert/wiki) | [Credits](https://github.com/trigger-death/TConvert/wiki/Credits) | [Image Album](http://imgur.com/a/QaoPd)

### [![Get TConvert](http://i.imgur.com/ImqTuVv.png)](https://github.com/trigger-death/TConvert/releases/tag/1.0.0.1)

## About

* **Created By:** Robert Jordan
* **Version:** 1.0.0.1
* **Language:** C#, WPF

## Requirements for Running
* .NET Framwork 4.5.2 | [Offline Installer](https://www.microsoft.com/en-us/download/details.aspx?id=42642) | [Web Installer](https://www.microsoft.com/en-us/download/details.aspx?id=42643)
* Windows 7 or later

## Building from Source
* Build with configuration *WinDebug* or *WinRelease* for the UI version.
* Build with configuration *ConDebug* or *ConRelease* for the pure console version.

## Features
* Extract image, sound, and font resources from Terraria's Xnb files, and extract songs from Terraria's Xwb wave bank.
* Convert images and sounds back into Xnb format and copy them to the content directory.
* Backup and restore your content folder for when you need to remove changes. (Glorified file copier)
* Run scripts that give more control over where files go when they are converted or extracted.
* Drop files into the window to automatically process them.
* Command line support for use with Windows Shell or the command prompt.

## About Xnb Format

Everything I learned about the Xnb format in order to read sprite fonts was gotten from the [documentation available on this page](http://xbox.create.msdn.com/en-us/sample/xnb_format). [Here's a mirror](http://www.mediafire.com/file/pf5dqw5dmup1msa/XNA_XNB_Format.zip) if that link ever goes down like old Microsoft links usually do.
