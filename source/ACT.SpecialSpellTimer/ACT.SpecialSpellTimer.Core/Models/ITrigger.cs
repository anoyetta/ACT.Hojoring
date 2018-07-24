using System;
using ACT.SpecialSpellTimer.Config.Models;

namespace ACT.SpecialSpellTimer.Models
{
    public interface ITrigger
    {
        ItemTypes ItemType { get; }

        void MatchTrigger(string logLine);
    }

    public static class TriggerExtensions
    {
        public static Guid GetID(
            this ITrigger t)
        {
            switch (t)
            {
                case SpellPanel p:
                    return p.ID;
                case Spell s:
                    return s.Guid;
                case Ticker ti:
                    return ti.Guid;
                case Tag tag:
                    return tag.ID;
                default:
                    return Guid.Empty;
            }
        }
    }
}
