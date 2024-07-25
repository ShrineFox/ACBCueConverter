﻿using System.Text;
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
                Console.WriteLine(e.Message);
                return;
            }

            Console.WriteLine(about);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var nameMap = GetNameMapFromACB();
            var awbMap = GetAwbMapFromACB();

            CopyFilesToNewDestination(nameMap, awbMap);
        }

        private static void CopyFilesToNewDestination(List<CueInfo> nameMap, List<CueInfo> awbMap)
        {
            foreach (var adx in Directory.GetFiles(options.InputDir, "*.adx", SearchOption.TopDirectoryOnly))
            {
                int adxId = Convert.ToInt32(Path.GetFileNameWithoutExtension(adx).Split('_')[0]);
                if (awbMap.Any(x => x.AwbId == adxId))
                {
                    foreach (var cueName in awbMap.First(x => x.AwbId == adxId).CueNames)
                    {
                        int cueNameId = nameMap.First(x => x.CueNames.Any(x => x.Equals(cueName))).AwbId;

                        string cueDir = Path.Combine(options.OutDir, cueNameId + ".cue");
                        Directory.CreateDirectory(cueDir);
                        string outFile = Path.Combine(cueDir, Path.GetFileName(adx));
                        File.Copy(adx, outFile, true);
                        string outFileConfigPath = Path.Combine(cueDir, Path.GetFileNameWithoutExtension(adx) + ".yaml");
                        File.WriteAllText(outFileConfigPath, "player_id: -1\nvolume: 1.0");
                    }
                }
            }
        }

        public static List<CueInfo> GetNameMapFromACB()
        {
            AcbFile loadedAcb = new AcbFile();
            loadedAcb.Load(options.AcbPath);

            var awbIDCueNamePairs = GetAwbIDCueNamePairs(loadedAcb.Cues);

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
                Console.WriteLine(newLine);
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

        private static List<CueInfo> GetAwbIDCueNamePairs(Dictionary<short, string> cues)
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
