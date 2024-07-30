using V3Lib.CriWare;

namespace ACBCueConverter
{
    internal partial class Program
    {
        // combined info for each track
        public class Adx
        {
            public string Path = "";
            public string CueName = "";
            public int WaveID = -1;
            public int CueID = -1;
            public bool Streaming = false;
        }

        // get Wave IDs and Cue Names from Text File
        public class Wave
        {
            public int ID = -1;
            public string Name = "";
        }

        // get Cue IDs and Cue Names from ACB
        public class Cue
        {
            public int ID = -1;
            public string Name = "";
        }

        private static List<Cue> GetCueNameCueIDPairsFromACB()
        {
            List<Cue> CueNameCueIDPairs = new List<Cue>();

            AcbFile loadedAcb = new AcbFile();
            loadedAcb.Load(options.Archive);
            foreach (var track in loadedAcb.Cues.OrderBy(x => x.Key))
                CueNameCueIDPairs.Add(new Cue() { ID = track.Key, Name = track.Value });
            Console.WriteLine("Got Cue Name/Cue ID pairs from ACB.");

            return CueNameCueIDPairs;
        }

        public static List<Wave> GetCueNameWaveIDPairsFromACB()
        {
            ACB acb = new ACB(options.Archive, new AudioCues() { });

            if (acb.TrackList == null)
            {
                Console.WriteLine($"Unable to read TrackList from ACB, using fallback text file: {Path.GetFileName(options.Text)}");
                return GetCueNameWaveIDPairsFromText(options.Text);
            }
            else
            {
                List<Wave> CueNameWaveIDPairs = new List<Wave>();

                foreach (var track in acb.TrackList.OrderBy(x => x.AwbId))
                    CueNameWaveIDPairs.Add(new Wave { ID = track.AwbId, Name = track.CueName });
                Console.WriteLine("Got Cue Name/Wave ID pairs from ACB.");

                return CueNameWaveIDPairs;
            }
        }

        private static List<Wave> GetCueNameWaveIDPairsFromText(string textPath)
        {
            List<Wave> CueNameWaveIDPairs = new List<Wave>();

            var lines = File.ReadAllLines(textPath);
            for (int i = 0; i < lines.Count(); i++)
            {
                // Format to TSV in case input is track title .txt dump using foobar2000 TextUtils plugin
                // https://www.foobar2000.org/components/view/foo_texttools
                string line = lines[i].TrimEnd(')').Replace(" (", "\t").Replace("; ", ";");

                foreach (var name in line.Split('\t')[1].Split(';').ToList())
                {
                    Wave wave = new Wave() { ID = i, Name = name };
                    CueNameWaveIDPairs.Add(wave);
                }
            }

            Console.WriteLine("Got Cue Name/Wave ID pairs from TXT/TSV.");

            return CueNameWaveIDPairs;
        }

        public static List<Adx> MatchedCueNames(List<Cue> cueList, List<Wave> waveList)
        {
            List<Adx> matchedCues = new List<Adx>();

            foreach (var wave in waveList)
            {
                if (cueList.Any(x => x.Name == wave.Name))
                {
                    var firstMatchingCue = cueList.First(x => x.Name == wave.Name);
                    Adx adx = new Adx() { CueID = firstMatchingCue.ID, CueName = wave.Name, WaveID = wave.ID };
                }
            }

            return matchedCues;
        }
    }
}
