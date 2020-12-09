using System;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public enum TimelineScriptEvents
    {
        /// <summary>
        /// 随時処理（デフォルト）
        /// </summary>
        /// <remarks>
        /// スクリプトは呼び出されるまで実行されない</remarks>
        Anytime = 0x00,

        /// <summary>
        /// 常駐処理
        /// </summary>
        Resident = 0x01,

        /// <summary>
        /// 判定を拡張する
        /// </summary>
        /// <remarks>
        /// t タグ の配下で使用した場合は自動的にこの扱いとなる
        /// </remarks>
        Expression = 0x02,

        /// <summary>
        /// Loglineが発生したとき
        /// </summary>
        /// <remarks>
        /// 読み取ったログは一定期間分をリスト化してグローバルオブジェクト経由でスクリプトに通知される。
        /// ログの発生1行ごとにスクリプトを呼び出した場合、オーバーヘッドが大きくなってしまう。したがって一定行数のログをまとめて呼び出す。
        /// スクリプト内では渡されたログに対してループして判定を行うこと。</remarks>
        OnLogs = 0x10,

        /// <summary>
        /// タイムラインファイルがロードされたとき
        /// </summary>
        OnLoad = 0x11,

        /// <summary>
        /// 当該サブルーチンが始まったとき
        /// </summary>
        OnSub = 0x12,

        /// <summary>
        /// ワイプしたとき
        /// </summary>
        OnWipeout = 0x14,
    }

    public interface ITimelineScript
    {
        public string Name { get; }

        public bool? Enabled { get; }

        public string ParentSubRoutine { get; }

        public TimelineScriptEvents? ScriptingEvent { get; set; }

        public double? Interval { get; set; }

        public DateTime LastExecutedTimestamp { get; }

        public string ScriptCode { get; set; }

        public bool Compile();

        public object Run();
    }
}
