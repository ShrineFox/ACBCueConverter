using System.Text;
using V3Lib.CriWare;
using ShrineFox.IO;
using System.Collections.ObjectModel;

// Using code from: https://github.com/DarkPsydeOfTheMoon/EVTUI
namespace ACBCueConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length == 0 || !File.Exists(args[0]))
            {
                ShowUsageInfo();
                return;
            }

            string acbPath = args[0];
            string awbPath = FileSys.GetExtensionlessPath(acbPath) + ".awb";
            if (File.Exists(awbPath))
                WriteTSVFromACB(acbPath, awbPath);
            else
                WriteTSVFromACB(acbPath);
        }

        public static void WriteTSVFromACB(string acbPath, string awbPath)
        {
            AcbFile loadedAcb = new AcbFile();
            loadedAcb.Load(acbPath);

            AwbFile loadedAwb = new AwbFile();
            loadedAwb.Load(awbPath);

            var awbIdCueNamePairs = GetAwbIDCueNamePairs(loadedAcb.Cues);
            var audio = loadedAwb.AudioData;

            string outTsvPath = FileSys.GetExtensionlessPath(acbPath) + ".tsv";
            WriteTSV(awbIdCueNamePairs, outTsvPath);
        }

        public static void WriteTSVFromACB(string acbPath)
        {
            ACB acb = new ACB(acbPath, new AudioCues() { });
            if (acb.TrackList == null)
            {
                Console.WriteLine("Unable to read TrackList from ACB, TrackList object is null.");
                return;
            }
            var awbIdCueNamePairs = GetAwbIDCueNamePairs(acb.TrackList);
            string outTsvPath = FileSys.GetExtensionlessPath(acbPath) + ".tsv";

            WriteTSV(awbIdCueNamePairs, outTsvPath);
        }

        public static void WriteTSV(List<CueInfo> awbIdCueNamePairs, string outTsvPath)
        {
            Console.WriteLine();

            string outTsvLines = "";
            foreach (var idPair in awbIdCueNamePairs)
            {
                string newLine = $"\n{idPair.AwbId}\t{string.Join(',', idPair.CueNames)}";
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

        public static void ShowUsageInfo()
        {
            Console.WriteLine("Outputs a TSV of Cue names to the directory of an ACB File." +
                    "\nUsage: ACBCueConverter.exe \"<PathToAcbFile>\"" +
                    "\nOutput: awb_id,cue_name" +
                    "\n\nPress any key to exit.");
            Console.ReadKey();
        }

        public class CueInfo
        {
            public int AwbId = -1;
            public List<string> CueNames = new List<string>();
        }
    }
}
