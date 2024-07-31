using System.Runtime.Serialization;
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
            [Option("t", "text", "file path", "Specifies the path to the TXT to get Wave IDs and Cue Names from.", Required = true)]
            public string Text { get; set; } = "";
            [Option("n", "namedFolders", "boolean", "If true, the Cue Name will be used for Ryo folders instead of the Cue ID.")]
            public bool NamedFolders { get; set; } = false;
            [Option("c", "categories", "string", "Value(s) to use for sound category (default: 2). (i.e. se: 0, bgm: 1, voice: 2, system: 3, syste_stream: 12)")]
            public string Categories { get; set; } = "2";
            [Option("v", "volume", "string", "Default volume setting for adx (default: 0.4). (0.4 = 40%)")]
            public string Volume { get; set; } = "0.4";
            [Option("an", "appendname", "string", "Text to append to the end of a Named Folder.")]
            public string AppendName { get; set; } = "";
            [Option("acbn", "acbname", "string", "Text to use for ACB name in .yaml.")]
            public string AcbName { get; set; } = "";
        }

        public class Adx
        {
            public string Path = "";
            public string CueName = "";
            public int WaveID = -1;
            public int CueID = -1;
            public bool Streaming = false;
        }

        static void Main(string[] args)
        {
            string about = SimpleCommandLineFormatter.Default.FormatAbout<ProgramOptions>("ShrineFox",
                "Copies ADX files exported from an ACB (via SonicAudioTools) to the Ryo Framework filesystem.");
            
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

            CopyFilesToNewDestination();
        }

        private static void CopyFilesToNewDestination()
        {
            // Get ADX data from .txt file
            List<Adx> AdxFiles = new List<Adx>();
            var lines = File.ReadAllLines(options.Text);
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                var partsArray = new string[] { parts[0], parts[1], parts[22], parts[27] };
                
                if (partsArray.Any(string.IsNullOrEmpty))
                    Console.WriteLine($"Line is missing information:\n\t{line}");
                else
                {
                    Adx adx = new Adx()
                    {
                        CueName = parts[0],
                        CueID = Convert.ToInt32(parts[1]),
                        WaveID = Convert.ToInt32(parts[22]),
                        Streaming = Convert.ToBoolean(parts[27])
                    };
                    AdxFiles.Add(adx);
                }
            }

            // Assign paths to ADX data
            foreach (var adxPath in Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly))
            {
                var pathParts = Path.GetFileName(adxPath).Split('_');
                int waveID = Convert.ToInt32(pathParts[0]);

                if (AdxFiles.Any(x => x.WaveID == waveID))
                {
                    if (pathParts[1].Equals("streaming"))
                    {
                        if (AdxFiles.Any(x => x.WaveID == waveID && x.Streaming == true))
                        {
                            foreach (var adx in AdxFiles.Where(x => x.WaveID == waveID && x.Streaming == true))
                                adx.Path = adxPath;
                        }
                        else
                            Console.WriteLine($"Wave ID {waveID} (streaming) could not be found.");
                    }
                    else
                    {
                        if (AdxFiles.Any(x => x.WaveID == waveID && x.Streaming == false))
                        {
                            foreach (var adx in AdxFiles.Where(x => x.WaveID == waveID && x.Streaming == false))
                                adx.Path = adxPath;
                        }
                        else
                            Console.WriteLine($"Wave ID (non-streaming) {waveID} could not be found.");
                    }
                }
                else
                    Console.WriteLine($"Wave ID {waveID} could not be found.");
            }

            var adxToMove = AdxFiles.Where(x => !string.IsNullOrEmpty(x.Path));
            // Copy files with non-null path to output folder
            foreach (var adx in adxToMove)
            {
                // Create output directory
                string cueDir = Path.Combine(options.OutDir, adx.CueID + ".cue");
                // Folders based on Cue Name (underscore at end to prevent being mistaken for Cue ID/Name)
                if (options.NamedFolders)
                    cueDir = Path.Combine(options.OutDir, adx.CueName + $"_{options.AppendName}");
                Directory.CreateDirectory(cueDir);
                // Copy adx to Cue ID folder
                string outFile = Path.Combine(cueDir, Path.GetFileName(adx.Path));
                File.Copy(adx.Path, outFile, true);
                // Create config file for .adx
                string configTxt = "";
                if (!string.IsNullOrEmpty(options.AcbName))
                    configTxt = $"acb_name: '{options.AcbName}'\n";
                configTxt += $"cue_name: '{adx.CueName}'\n" +
                    $"player_id: -1\n" +
                    $"volume: {options.Volume}\n" +
                    $"category_ids: [{options.Categories}]";
                string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx.Path) + ".yaml");
                File.WriteAllText(outFileConfigPath, configTxt);
            }

            Console.WriteLine("Done copying files to new destination." +
                "\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
