using System.Text;

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
                Console.WriteLine("Outputs a TSV of Cue names to the directory of an ACB File." +
                    "\nUsage: ACBCueConverter.exe \"<PathToAcbFile>\"" +
                    "\nOutput: awb_id,cue_name" +
                    "\n\nPress any key to exit.");
                Console.ReadKey();
                return;
            }

            string sourceAcb = args[0];

            ACB acb = new ACB(sourceAcb, new AudioCues() { });
            if (acb.TrackList == null)
            {
                Console.WriteLine("Unable to read TrackList from ACB, TrackList object is null.");
                return;
            }

            Console.WriteLine();
            string outTsvLines = "";
            foreach (var track in acb.TrackList.OrderBy(x => x.AwbId))
            {
                if (!outTsvLines.Contains($"\n{track.AwbId}\t"))
                {
                    string newLine = $"\n{track.AwbId}\t{track.CueName}";
                    Console.WriteLine(newLine);
                    outTsvLines += newLine;
                }
                else
                {
                    string newLine = $",{track.CueName}";
                    Console.WriteLine(newLine);
                    outTsvLines += newLine;
                }
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(sourceAcb), 
                Path.GetFileNameWithoutExtension(sourceAcb) + ".tsv"), outTsvLines);
        }
    }
}
