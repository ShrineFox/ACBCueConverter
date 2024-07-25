using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ACBCueConverter;

public struct AudioCues
{
    public AudioCues()
    {
        EnCues = new LocaleCues();
        JpCues = new LocaleCues();
    }

    public LocaleCues EnCues { get; set; }
    public LocaleCues JpCues { get; set; }
}

public struct LocaleCues
{
    public LocaleCues()
    {
        EventVoice = new Dictionary<uint, MessageCue>();
        EventSFX = new Dictionary<uint, MessageCue>();
        Common = new Dictionary<uint, MessageCue>();
        Field = new Dictionary<uint, MessageCue>();
    }

    public Dictionary<uint, MessageCue> EventVoice { get; set; }
    public Dictionary<uint, MessageCue> EventSFX { get; set; }
    public Dictionary<uint, MessageCue> Common { get; set; }
    public Dictionary<uint, MessageCue> Field { get; set; }
}

public class MessageCue
{
    public MessageCue(string speakerName, string turnName, int indWithinTurn)
    {
        SpeakerName = speakerName;
        TurnName = turnName;
        IndWithinTurn = indWithinTurn;
    }

    public string SpeakerName { get; set; }
    public string TurnName { get; set; }
    public int IndWithinTurn { get; set; }
    public (uint Lower, uint Upper) CueRange { get; set; }

    public string Stringification { get { return $"{SpeakerName} - {TurnName} ({IndWithinTurn})"; } }
}