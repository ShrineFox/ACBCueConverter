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
            [Option("a", "acbPath", "acb path", "Specifies the path to the ACB file to get Cue Names and AWB IDs from.", Required = true)]
            public string AcbPath { get; set; } = "";
            [Option("i", "inputDir", "directory path", "Specifies the path to the directory with FEmulator .adx to copy.", Required = true)]
            public string InputDir { get; set; } = "";
            [Option("o", "outDir", "directory path", "Specifies the path to the Ryo ACB directory to output .adx to.", Required = true)]
            public string OutDir { get; set; } = "";
            [Option("f", "fallbackTsv", "tsv path", "Specifies the path to the .txt or .tsv to use for mapping AWB IDs to Cue Names.")]
            public string FallbackTsv { get; set; } = "";
            [Option("c", "cueNames", "boolean", "If true, the Cue Name will be used for Ryo folders instead of the Cue ID.")]
            public bool CueNames { get; set; } = false;
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
        }

        /*
        
        awbMap.tsv
        AWB => CUE NAMES
        the 0th ADX in the AWB is assigned to a Cue named 118.
        That means 00000_streaming.adx needs to be copied to ???.cue

        nameMap.tsv
        CUE ID => CUE NAME
        The cue named 118 has cue ID 22.
        That means 00000_streaming.adx needs to be copied to 22.cue

        */

        private static void CopyFilesToNewDestination(List<CueInfo> nameMap, List<CueInfo> awbMap)
        {
            // For each adx file in input folder...
            foreach (var adx in Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly))
            {
                // Get the _streaming ID (awb ID).
                int adxId = Convert.ToInt32(Path.GetFileNameWithoutExtension(adx).Split('_')[0]);
                
                // If AWB ID is in list mapping it to cue name...
                if (awbMap.Any(x => x.AwbId == adxId))
                {
                    // Get each cue name from list
                    foreach (var cueName in awbMap.First(x => x.AwbId == adxId).CueNames)
                    {
                        // Get ID of Cue based on name
                        int cueNameId = nameMap.First(x => x.CueNames.Any(x => x.Equals(cueName))).AwbId;
                        // Create folder named after Cue ID (or Cue Name depending on settings)
                        string cueDir = Path.Combine(options.OutDir, cueNameId + ".cue");
                        if (options.CueNames)
                        {
                            // the underscore keeps it from being mistaken for a Cue ID
                            cueDir = Path.Combine(options.OutDir, cueName + "_"); 
                        }
                        Directory.CreateDirectory(cueDir);
                        // Copy adx to Cue ID folder
                        string outFile = Path.Combine(cueDir, Path.GetFileName(adx));
                        File.Copy(adx, outFile, true);
                        // Create config file for .adx
                        string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx) + ".yaml");
                        File.WriteAllText(outFileConfigPath, 
                            $"acb_name: '{Path.GetFileNameWithoutExtension(options.AcbPath)}'\n" +
                            $"cue_name: '{cueNameId}'\n" +
                            $"player_id: -1\n" +
                            $"volume: 1.0" );
                    }
                }
            }
        }

        public static List<CueInfo> GetNameMapFromACB()
        {
            AcbFile loadedAcb = new AcbFile();
            loadedAcb.Load(options.AcbPath);

            var awbIDCueNamePairs = GetCueNameIDPairs(loadedAcb.Cues);

            if (options.Debug)
            {
                string outTsvPath = FileSys.GetExtensionlessPath(options.AcbPath) + "_nameMap.tsv";
                WriteTSV(awbIDCueNamePairs, outTsvPath);
            }

            return awbIDCueNamePairs;
        }

        public static List<CueInfo> GetAwbMapFromACB()
        {
            ACB acb = new ACB(options.AcbPath, new AudioCues() { });
            if (acb.TrackList == null)
            {
                Console.WriteLine($"Unable to read TrackList from ACB, using fallback: {Path.GetFileName(options.AcbPath)}");
                
                return GetCueListFromTsv(options.FallbackTsv);
            }
            var awbMap = GetAwbIDCueNamePairs(acb.TrackList);

            if (options.Debug)
            {
                string outTsvPath = FileSys.GetExtensionlessPath(options.AcbPath) + "_awbMap.tsv";
                WriteTSV(awbMap, outTsvPath);
            }

            return awbMap;

        }

        public static void WriteTSV(List<CueInfo> awbIdCueNamePairs, string outTsvPath)
        {
            Console.WriteLine();

            string outTsvLines = "";
            foreach (var idPair in awbIdCueNamePairs)
            {
                string newLine = $"\n{idPair.AwbId}\t{string.Join(';', idPair.CueNames)}";
                //Console.WriteLine(newLine);
                outTsvLines += newLine;
            }

            File.WriteAllText(outTsvPath, outTsvLines);
        }

        public static List<CueInfo> GetAwbIDCueNamePairs(ObservableCollection<TrackEntry> trackList)
        {
            List<CueInfo> awbIdCueNamePairs = new List<CueInfo>();

            foreach (var track in trackList.OrderBy(x => x.AwbId))
            {
                if (awbIdCueNamePairs.Any(x => x.AwbId == track.AwbId))
                    awbIdCueNamePairs.First(x => x.AwbId == track.AwbId).CueNames.Add(track.CueName);
                else
                    awbIdCueNamePairs.Add(new CueInfo { AwbId = track.AwbId, CueNames = { track.CueName }  });
            }

            return awbIdCueNamePairs;
        }

        private static List<CueInfo> GetCueNameIDPairs(Dictionary<short, string> cues)
        {
            List<CueInfo> awbIdCueNamePairs = new List<CueInfo>();

            foreach (var track in cues.OrderBy(x => x.Key))
            {
                if (awbIdCueNamePairs.Any(x => x.AwbId == Convert.ToInt32(track.Key)))
                    awbIdCueNamePairs.First(x => x.AwbId == Convert.ToInt32(track.Key)).CueNames.Add(track.Value);
                else
                    awbIdCueNamePairs.Add(new CueInfo { AwbId = Convert.ToInt32(track.Key), CueNames = { track.Value } });
            }

            return awbIdCueNamePairs;
        }

        private static List<CueInfo> GetCueListFromTsv(string tsvPath)
        {
            List<CueInfo> cues = new List<CueInfo>();

            var lines = File.ReadAllLines(tsvPath);
            for (int i = 0; i < lines.Count(); i++)
            {
                string line = lines[i].TrimEnd(')').Replace(" (", "\t").Replace("; ", ";");

                CueInfo cue = new CueInfo();

                cue.AwbId = i;
                cue.CueNames = line.Split('\t')[1].Split(';').ToList();

                cues.Add(cue);
            }

            return cues;
        }

        public class CueInfo
        {
            public int AwbId = -1;
            public List<string> CueNames = new List<string>();
        }
    }
}
