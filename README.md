# ACBCueConverter
Copies ADX files exported from an ACB (via ACE) to the Ryo Framework filesystem.

# Commandline Usage
Use [ACE](https://github.com/LazyBone152/ACE/releases) to extract the raw contents of an AWB to a folder.  
Then, run this command to convert it for use with [Ryo Framework](https://gamebanana.com/mods/495507).  
``ACBCueConverter.exe -i <C:\ACEOutput\> -o <C:\Mod\Ryo\SYSTEM.ACB\> -n -c 3 -v 0.5``
## Parameters
```
"-i" - Input Directory. Specifies the path to the directory with FEmulator .adx to copy. (required)
"-o" - Output Directory. Specifies the path to the Ryo ACB directory to output .adx to. (required)
"-n" - Named Folders. If true, the Cue Name will be used for Ryo folders instead of the Cue ID. (default: false)
"-c" - Categories. Value(s) to use for sound category. (i.e. se: 0, bgm: 1, voice: 2, system: 3, syste_stream: 12) (default: 2) 
"-v" - Volume. Default volume setting for adx. (ex. 0.4 = 40%) (default: 0.4)
```
# Known Issues
- Only intended for P5R PC at the moment, doesn't support .hca yet.
