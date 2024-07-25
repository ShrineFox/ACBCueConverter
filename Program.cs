using System.Text;
using V3Lib.CriWare;
using ShrineFox.IO;
using System.Collections.ObjectModel;
using TGE.SimpleCommandLine;
using static System.Windows.Forms.Design.AxImporter;
using System.Windows.Forms;

// Using code from: https://github.com/DarkPsydeOfTheMoon/EVTUI
namespace ACBCueConverter
{
    internal class Program
    {
        public static ProgramOptions options;

        public class ProgramOptions
        {
            [Option("a", "acbPath", "acb path", "Specifies the path to the ACB file to get Cue Names and Wave IDs from.", Required = true)]
            public string AcbPath { get; set; } = "";
            [Option("i", "inputDir", "directory path", "Specifies the path to the directory with FEmulator .adx to copy.", Required = true)]
            public string InputDir { get; set; } = "";
            [Option("o", "outDir", "directory path", "Specifies the path to the Ryo ACB directory to output .adx to.", Required = true)]
            public string OutDir { get; set; } = "";
            [Option("f", "fallbackTsv", "tsv path", "Specifies the path to the .txt or .tsv to use for mapping Wave IDs to Cue Names.")]
            public string FallbackTsv { get; set; } = "";
            [Option("n", "namedFolders", "boolean", "If true, the Cue Name will be used for Ryo folders instead of the Cue ID.")]
            public bool NamedFolders { get; set; } = false;
            [Option("c", "categories", "string", "Add value(s) to use for sound category. (i.e. se: 0, bgm: 1, voice: 2, system: 3, syste_stream: 12)")]
            public string Categories { get; set; } = "";
            [Option("v", "volume", "string", "Default volume setting for adx. (1.0 = 100%)")]
            public string Volume { get; set; } = "1.0";
            [Option("m", "mappingTxt", "txt path", "If used, lines from the text file will be used to name Ryo folders.")]
            public string MappingTxt { get; set; } = "";
            [Option("s", "swap", "boolean", "If true, read Wave IDs from the end of filename (ACE output).")]
            public bool Swap { get; set; } = false;
            [Option("of", "offset", "interger", "Add this amount to the start of the Cue Name ID mapping.")]
            public int Offset { get; set; } = 0;
            [Option("d", "debug", "boolean", "If true, TSVs for processed ACB files will be output next to their locations.")]
            public bool Debug { get; set; } = false;
        }

        static void Main(string[] args)
        {
            string about = SimpleCommandLineFormatter.Default.FormatAbout<ProgramOptions>("ShrineFox",
                "Outputs ADX files from AWBEmulator format to Ryo format based on a TSV file.");
            
            try
            {
                options = SimpleCommandLineParser.Default.Parse<ProgramOptions>(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(about);
                Console.WriteLine(e.Message);
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var nameMap = GetNameMapFromACB();
            var awbMap = GetAwbMapFromACB();

            CopyFilesToNewDestination(nameMap, awbMap);
            RenameFoldersFromMapTxt();
        }

        private static void RenameFoldersFromMapTxt()
        {
            string[] mappedNames = new string[] { };
            if (File.Exists(options.MappingTxt))
                mappedNames = File.ReadAllLines(options.MappingTxt);
            else
                return;

            var folders = Directory.GetDirectories(options.OutDir);

            for (int i = 0; i < mappedNames.Length; i++)
            {
                string destination = Path.Combine(options.OutDir, mappedNames[i]);
                if (!Directory.Exists(destination))
                    Directory.Move(folders[i], destination);
            }
            Console.WriteLine("Done renaming output folders based on text file.");
        }

        private static void CopyFilesToNewDestination(List<CueInfo> nameMap, List<CueInfo> awbMap)
        {
            // Get adx files in input directory
            var adxFiles = Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly);

            // For each adx file in input folder...
            foreach (var adx in adxFiles)
            {
                // Get the _streaming ID (awb ID).
                int adxId = -1;
                if (options.Swap) // Optionally read ACE output filename format
                    adxId = Convert.ToInt32(Path.GetFileNameWithoutExtension(adx).Split('(').Last().TrimEnd(')'));
                else // otherwise use SonicAudioTools output filename format
                    adxId = Convert.ToInt32(Path.GetFileNameWithoutExtension(adx).Split('_')[0]);
                
                // If AWB ID is in list mapping it to cue name...
                if (awbMap.Any(x => x.WaveID == adxId))
                {
                    // Get each cue name from list
                    foreach (var cueName in awbMap.First(x => x.WaveID == adxId).CueNames)
                    {
                        // Get ID of Cue based on name
                        int cueNameId = nameMap.First(x => x.CueNames.Any(x => x.Equals(cueName))).WaveID;
                        // Create folder named after Cue ID (or Cue Name depending on settings)
                        string cueDir = Path.Combine(options.OutDir, cueNameId + ".cue");
                        if (options.NamedFolders)
                        {
                            // the underscore keeps it from being mistaken for a Cue ID
                            cueDir = Path.Combine(options.OutDir, cueName + "_"); 
                        }
                        Directory.CreateDirectory(cueDir);
                        // Copy adx to Cue ID folder
                        string outFile = Path.Combine(cueDir, Path.GetFileName(adx));
                        File.Copy(adx, outFile, true);
                        // Create config file for .adx
                        string configTxt = $"acb_name: '{Path.GetFileNameWithoutExtension(options.AcbPath)}'\n" +
                            $"cue_name: '{cueNameId}'\n" +
                            $"player_id: -1\n" +
                            $"volume: {options.Volume}";
                        if (!string.IsNullOrEmpty(options.Categories))
                            configTxt += $"\ncategory_ids: [{options.Categories}]";
                        string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx) + ".yaml");
                        File.WriteAllText(outFileConfigPath, configTxt);
                    }
                }
            }
            Console.WriteLine("Done copying files to new destination.");
        }

        public static List<CueInfo> GetNameMapFromACB()
        {
            AcbFile loadedAcb = new AcbFile();
            loadedAcb.Load(options.AcbPath);

            var WaveIDCueNamePairs = GetCueNameIDPairs(loadedAcb.Cues);

            if (options.Debug)
            {
                string outTsvPath = FileSys.GetExtensionlessPath(options.AcbPath) + "_nameMap.tsv";
                WriteTSV(WaveIDCueNamePairs, outTsvPath);
            }

            Console.WriteLine("Got Cue Name/Cue ID pairs from ACB.");
            return WaveIDCueNamePairs;
        }

        public static List<CueInfo> GetAwbMapFromACB()
        {
            ACB acb = new ACB(options.AcbPath, new AudioCues() { });
            if (acb.TrackList == null)
            {
                Console.WriteLine($"Unable to read TrackList from ACB, using fallback: {Path.GetFileName(options.AcbPath)}");
                
                return GetCueListFromTsv(options.FallbackTsv);
            }
            var awbMap = GetWaveIDCueNamePairs(acb.TrackList);

            if (options.Debug)
            {
                string outTsvPath = FileSys.GetExtensionlessPath(options.AcbPath) + "_awbMap.tsv";
                WriteTSV(awbMap, outTsvPath);
            }

            Console.WriteLine("Got Cue Name/Wave ID pairs from ACB.");

            return awbMap;
        }

        public static void WriteTSV(List<CueInfo> WaveIDCueNamePairs, string outTsvPath)
        {
            string outTsvLines = "";
            foreach (var idPair in WaveIDCueNamePairs)
            {
                string newLine = $"\n{idPair.WaveID}\t{string.Join(';', idPair.CueNames)}";
                //Console.WriteLine(newLine);
                outTsvLines += newLine;
            }

            File.WriteAllText(outTsvPath, outTsvLines);
        }

        public static List<CueInfo> GetWaveIDCueNamePairs(ObservableCollection<TrackEntry> trackList)
        {
            List<CueInfo> WaveIDCueNamePairs = new List<CueInfo>();

            foreach (var track in trackList.OrderBy(x => x.WaveID))
            {
                if (WaveIDCueNamePairs.Any(x => x.WaveID == track.WaveID))
                    WaveIDCueNamePairs.First(x => x.WaveID == track.WaveID).CueNames.Add(track.CueName);
                else
                    WaveIDCueNamePairs.Add(new CueInfo { WaveID = track.WaveID, CueNames = { track.CueName }  });
            }

            return WaveIDCueNamePairs;
        }

        private static List<CueInfo> GetCueNameIDPairs(Dictionary<short, string> cues)
        {
            List<CueInfo> WaveIDCueNamePairs = new List<CueInfo>();

            foreach (var track in cues.OrderBy(x => x.Key))
            {
                if (WaveIDCueNamePairs.Any(x => x.WaveID == Convert.ToInt32(track.Key + options.Offset)))
                    WaveIDCueNamePairs.First(x => x.WaveID == Convert.ToInt32(track.Key + options.Offset)).CueNames.Add(track.Value);
                else
                    WaveIDCueNamePairs.Add(new CueInfo { WaveID = Convert.ToInt32(track.Key + options.Offset), CueNames = { track.Value } });
            }

            return WaveIDCueNamePairs;
        }

        private static List<CueInfo> GetCueListFromTsv(string tsvPath)
        {
            List<CueInfo> cues = new List<CueInfo>();

            var lines = File.ReadAllLines(tsvPath);
            for (int i = 0; i < lines.Count(); i++)
            {
                // Format to TSV in case input is track title .txt dump using foobar2000 TextUtils plugin
                // https://www.foobar2000.org/components/view/foo_texttools
                string line = lines[i].TrimEnd(')').Replace(" (", "\t").Replace("; ", ";");

                CueInfo cue = new CueInfo();

                cue.WaveID = i;
                cue.CueNames = line.Split('\t')[1].Split(';').ToList();

                cues.Add(cue);
            }

            Console.WriteLine("Got Cue Name/Wave ID pairs from TXT/TSV.");

            return cues;
        }

        public class CueInfo
        {
            public int WaveID = -1;
            public List<string> CueNames = new List<string>();

        }

        public class Cue
        {
            public string Name = "";
            public short ID = -1;
            public List<int> WaveIDs = new List<int>();
        }
    }
}
