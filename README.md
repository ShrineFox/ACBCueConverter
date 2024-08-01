# ACBCueConverter
Copies ADX files exported from an ACB (via SonicAudioTools) to the [Ryo Framework](https://gamebanana.com/mods/495507) filesystem.

# Commandline Usage
1. Use [ACBEditor](https://github.com/blueskythlikesclouds/SonicAudioTools/releases) to extract the raw contents of an AWB to a folder
   OR use PersonaVCE to prefix your .ADX filenames with "WAVEID_streaming_" (or just "WAVEID_" if not a streaming track). e.x. ``00123_streaming_whatever.adx``
2. Get a ``.tsv`` (tab separated values) file for the ``.acb`` you're editing by dragging the original ``.acb`` archive into [CriAtomViewer](https://game.criware.jp/products/adx-le/). (extension must be lowercase!)  
   Click inside the table and press CTRL + A to highlight everything. Then right click and copy as tabs delineated text. Paste into a text file and save.
3. Open the command prompt at the location of ``ACBCueConverter.exe`` and use the commands below to generate your [Ryo Framework](https://gamebanana.com/mods/495507) output folder.

Example:
``ACBCueConverter.exe -i C:\ACEOutput\ -o C:\Mod\Ryo\SYSTEM.ACB\ -t C:\p5r_system_acb.txt -n true -an "Joker" -c 2 -v 0.4 -acbn "system"``  
- This will take the ``.adx`` from the input folder and get their WAVE IDs from the filename.  
- Then, folders will be created in the output folder named after the CUE Names (since ``-n`` is true, otherwise folders would be named ``<CUEID>.cue``.
- Because ``-an`` is ``Joker``, the word ``Joker`` will be appended to the folder names (this is useful assuming the input files provided are only for Joker's voicelines).
- Volume for each clip will be set to ``0.4`` in the respective clip's ``config.yaml`` because of the ``-v 0.4`` command.
- Similarly, Category will be set to 2 (voice) in order to use the correct sound player properties (i.e. the volume set by the user ingame, the number of concurrent players etc.)
- Because the name of the ACB was specified by ``-acbn``, each sound clip's config will also include ``system`` as the ``acb_name``. This is only useful if output is not in a folder named after the ACB.

## Parameters
```
"-i" - Input Directory. Specifies the path to the directory with FEmulator .adx to copy. (required)
"-o" - Output Directory. Specifies the path to the Ryo ACB directory to output .adx to. (required)
"-t" - Text File. Specifies the path to the TXT to get Wave IDs and Cue Names from. (required)
"-n" - Named Folders. If true, the Cue Name will be used for Ryo folders instead of the Cue ID. (default: false)
"-c" - Categories. Value(s) to use for sound category. (i.e. se: 0, bgm: 1, voice: 2, system: 3, syste_stream: 12) (default: 2) 
"-v" - Volume. Default volume setting for adx. (ex. 0.4 = 40%) (default: 0.4)
"-an" - Append Name. Text to append to the end of a Named Folder in the Ryo output directory. (ex. "Joker" for "btl_support_Joker")
"-acbn" - ACB Name. Text to use for ACB name in .yaml. (ex. "SYSTEM" for SYSTEM.AWB)
```
