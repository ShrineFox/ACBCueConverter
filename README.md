# ACBCueConverter
Outputs a TSV of Cue names to the directory of an ACB File

# Commandline Usage
1. Drag/drop an ``.ACB`` file onto ``ACBCueConverter.exe``.
2. A ``.TSV`` (tab-separated values) file will be generated. ``Line 1: AWB Index, Line 2: Cue Name``
3. Do whatever you want with the data, such as renaming files extracted from a ``.AWB`` to their Cue Name.

Note: Cue Names shared by multiple AWB Indices will be appended to Line 2 of the first AWB Index, separated by commas.

# Example Output
```
...
367	bp01_p_169
368	bp01_p_201
369	bp01_p_170,bp01_p_199
370	bp01_p_252
371	bp01_p_253
...
```

# Known Issues
- Only works with ACB files that don't contain a mix of streaming and non-streaming audio.

# Credit
[EVTUI](https://github.com/DarkPsydeOfTheMoon/EVTUI) - Ported ACB parsing code
[XV2-Tools](https://github.com/LazyBone152/XV2-Tools) - Original ACB parsing code

