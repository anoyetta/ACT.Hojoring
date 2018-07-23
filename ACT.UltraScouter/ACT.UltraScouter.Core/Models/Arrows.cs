using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ACT.UltraScouter.Models
{
    public enum Direction
    {
        Unknown = -1,
        E,
        SE,
        S,
        SW,
        W,
        NW,
        N,
        NE
    }

    public static class Arrows
    {
        public static readonly FontFamily FontAwesome = new FontFamily(new Uri("pack://application:,,,/FontAwesome.WPF;component/"), "./#FontAwesome");

        /// <summary>
        /// 矢印の基準（Wingdings版）
        /// </summary>
        /// <remarks>
        /// 矢印の基準は数学的X0, Y0である"→"とする</remarks>
        public const string Arrow0ByWingdings = Arrows.E;

        /// <summary>
        /// 矢印の基準（FontAwesome版）
        /// </summary>
        /// <remarks>
        /// 矢印の基準は数学的X0, Y0である"→"とする</remarks>
        public const string Arrow0ByFontAwesome = "\xf061";

        public const string Unknown = "";
        public const string N = "\xE9";
        public const string NE = "\xEC";
        public const string E = "\xE8";
        public const string SE = "\xEE";
        public const string S = "\xEA";
        public const string SW = "\xED";
        public const string W = "\xE7";
        public const string NW = "\xEB";

        public static readonly Dictionary<Direction, string> ArrowDictionary = new Dictionary<Direction, string>()
        {
            { Direction.Unknown, Arrows.Unknown },
            { Direction.N, Arrows.N },
            { Direction.NE, Arrows.NE },
            { Direction.E, Arrows.E },
            { Direction.SE, Arrows.SE },
            { Direction.S, Arrows.S },
            { Direction.SW, Arrows.SW },
            { Direction.W, Arrows.W },
            { Direction.NW, Arrows.NW },
        };

        public static string ToArrow(
            this Direction d) =>
            ArrowDictionary.ContainsKey(d) ?
            ArrowDictionary[d] :
            Arrows.Unknown;
    }
}
