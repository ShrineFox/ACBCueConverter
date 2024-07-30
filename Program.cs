using TGE.SimpleCommandLine;

namespace ACBCueConverter
{
    internal partial class Program
    {
        public static ProgramOptions options;

        public class ProgramOptions
        {
            [Option("i", "inputDir", "directory path", "Specifies the path to the directory with FEmulator .adx to copy.", Required = true)]
            public string InputDir { get; set; } = "";
            [Option("o", "outDir", "directory path", "Specifies the path to the Ryo ACB directory to output .adx to.", Required = true)]
            public string OutDir { get; set; } = "";
            [Option("n", "namedFolders", "boolean", "If true, the Cue Name will be used for Ryo folders instead of the Cue ID.")]
            public bool NamedFolders { get; set; } = false;
            [Option("c", "categories", "string", "Value(s) to use for sound category (default: 2). (i.e. se: 0, bgm: 1, voice: 2, system: 3, syste_stream: 12)")]
            public string Categories { get; set; } = "2";
            [Option("v", "volume", "string", "Default volume setting for adx (default: 0.4). (0.4 = 40%)")]
            public string Volume { get; set; } = "0.4";
            [Option("a", "archive", "file path", "Specifies the path to the ACB to get Cue IDs and Cue Names from.")]
            public string Archive { get; set; } = "";
            [Option("t", "text", "file path", "Specifies the path to the TXT to get Wave IDs and Cue Names from.")]
            public string Text { get; set; } = "";

        }

        static void Main(string[] args)
        {
            string about = SimpleCommandLineFormatter.Default.FormatAbout<ProgramOptions>("ShrineFox",
                "Copies ADX files exported from an ACB (via ACE) to the Ryo Framework filesystem.");
            
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

            if (!File.Exists(options.Archive))
                CopyACEFilesToNewDestination();
            else
                CopySonicAudioToolsFilesToNewDestination();
        }

        private static void CopySonicAudioToolsFilesToNewDestination()
        {
            // Get list of Wave IDs matched to Cue IDs by name
            var AcbEntries = MatchedCueNames(GetCueNameCueIDPairsFromACB(), GetCueNameWaveIDPairsFromACB());

            // Get adx files in input directory
            var adxPaths = Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly);

            List<Adx> NewAcbEntries = new List<Adx>();

            // For each adx file in input folder...
            foreach (var adxPath in adxPaths)
            {
                // Get Wave ID from filename
                var Adx = new Adx() { Path = adxPath };
                Adx.WaveID = Convert.ToInt32(Path.GetFileNameWithoutExtension(adxPath).Split('_')[0]);

                // Get whether it's streamed or not from filename
                if (Path.GetFileNameWithoutExtension(adxPath).Split('_')[0] == "streaming")
                    Adx.Streaming = true;

                // Add to new list of entries if it matches an existing record by Wave ID
                if (AcbEntries.Any(x => x.WaveID == Adx.WaveID))
                {
                    var matchingEntry = AcbEntries.First(x => x.WaveID == Adx.WaveID);
                    Adx.CueName = matchingEntry.CueName;
                    Adx.CueID = matchingEntry.CueID;

                    NewAcbEntries.Add(Adx);
                }
            }

            foreach (var adx in NewAcbEntries.OrderBy(x => x.CueID))
            {
                // Create output directory
                string cueDir = Path.Combine(options.OutDir, adx.CueID + ".cue");
                // Folders retain Cue Name (underscore at end to prevent being mistaken for Cue ID)
                if (options.NamedFolders)
                    cueDir = Path.Combine(options.OutDir, adx.CueName + "_");
                Directory.CreateDirectory(cueDir);
                // Copy adx to Cue ID folder
                string outFile = Path.Combine(cueDir, Path.GetFileName(adx.Path));
                File.Copy(adx.Path, outFile, true);
                // Create config file for .adx
                string configTxt = $"cue_name: '{adx.CueID}'\n" +
                    $"player_id: -1\n" +
                    $"volume: {options.Volume}\n" +
                    $"category_ids: [{options.Categories}]";
                string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx.Path) + ".yaml");
                File.WriteAllText(outFileConfigPath, configTxt);
            }

            Console.WriteLine("Done copying files to new destination.");
        }

        private static void CopyACEFilesToNewDestination()
        {
            // Get adx files in input directory
            var adxPaths = Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly);

            var AdxFiles = new List<Adx>();
            // For each adx file in input folder...
            foreach (var adxPath in adxPaths)
            {
                // Get details from filename (ACE Output style)
                Adx adxDetails = new Adx();
                
                adxDetails.Path = adxPath;
                string[] pathParts = Path.GetFileNameWithoutExtension(adxPath).Split('_');
                adxDetails.CueID = Convert.ToInt16(pathParts[0]);
                adxDetails.WaveID = Convert.ToInt16(pathParts.Last().TrimStart('(').TrimEnd(')'));
                adxDetails.CueName = Path.GetFileNameWithoutExtension(adxPath)
                    .Replace($"{adxDetails.CueID}_", "").Replace($"_({adxDetails.WaveID})", "");
                
                AdxFiles.Add(adxDetails);
            }

            foreach (var adx in AdxFiles.OrderBy(x => x.CueID))
            {
                // Create output directory
                string cueDir = Path.Combine(options.OutDir, adx.CueID + ".cue");
                // Folders retain Cue Name (underscore at end to prevent being mistaken for Cue ID)
                if (options.NamedFolders)
                    cueDir = Path.Combine(options.OutDir, adx.CueName + "_");
                Directory.CreateDirectory(cueDir);
                // Copy adx to Cue ID folder
                string outFile = Path.Combine(cueDir, Path.GetFileName(adx.Path));
                File.Copy(adx.Path, outFile, true);
                // Create config file for .adx
                string configTxt = $"cue_name: '{adx.CueID}'\n" +
                    $"player_id: -1\n" +
                    $"volume: {options.Volume}\n" +
                    $"category_ids: [{options.Categories}]";
                string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx.Path) + ".yaml");
                File.WriteAllText(outFileConfigPath, configTxt);
            }

            Console.WriteLine("Done copying files to new destination.");
        }

    }
}
