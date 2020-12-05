using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Sound;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// テキストコマンド Controller
    /// </summary>
    public static class TextCommandController
    {
        /// <summary>
        /// コマンド解析用の正規表現
        /// </summary>
        private readonly static Regex regexCommand = new Regex(
            @"/spespe\s+(?<command>refresh|changeenabled|set|clear|on|off|pos)\s+?(?<target>all|spells|telops|me|pt|pet|placeholder|$)\s+?(?<windowname>"".*""|all)? ?(?<value>.*)",
            RegexOptions.Compiled);

        /// <summary>
        /// TTS読み仮名コマンド
        /// </summary>
        private readonly static Regex phoneticsCommand = new Regex(
            @"/spespe\s+phonetic\s+""(?<pcname>.+?)"" ""(?<phonetic>.+?)""",
            RegexOptions.Compiled);

        /// <summary>
        /// Logコマンド
        /// </summary>
        private readonly static Regex logCommand = new Regex(
            @"/spespe\s+log\s+(?<switch>on|off|open|flush)",
            RegexOptions.Compiled);

        /// <summary>
        /// TTSコマンド
        /// </summary>
        private readonly static Regex ttsCommand = new Regex(
            @"/tts\s+(?<text>.*)",
            RegexOptions.Compiled);

        /// <summary>
        /// オプションコマンド
        /// </summary>
        private static readonly Lazy<Regex> lazyOptionCommand = new Lazy<Regex>(() => new Regex(
            @$"/spespe\s+(?<option>{string.Join("|", optionCommands.Select(x => x.keyword))})\s+(?<command>enabled|disabled|on|off)",
            RegexOptions.Compiled));

        /// <summary>
        /// オプションコマンドの定義
        /// </summary>
        private static readonly (string keyword, Action<bool> change)[] optionCommands = new[]
        {
            ("reset-on-wipeout", new Action<bool>((value) => Settings.Default.ResetOnWipeOut = value))
        };

        /// <summary>
        /// ログ1行とマッチングする
        /// </summary>
        /// <param name="logLine">ログ行</param>
        /// <returns>
        /// エコーを鳴らすか？</returns>
        public static bool MatchCommandCore(
            string logLine)
        {
            logLine = logLine.ToLower();

            // 正規表現の前にキーワードがなければ抜けてしまう
            if (!logLine.Contains("/spespe") &&
                !logLine.Contains("/tts"))
            {
                return false;
            }

            var inCombat = XIVPluginHelper.Instance.InCombat;

            // 読み仮名コマンドとマッチングする
            if (MatchPhoneticCommand(logLine))
            {
                return true;
            }

            // ログコマンドとマッチングする
            if (MatchLogCommand(logLine))
            {
                return true;
            }

            // スペスペオプションの操作コマンドとマッチングする
            if (MatchOptionCommand(logLine))
            {
                // 戦闘中ならば鳴らさない
                return !inCombat;
            }

            // TTSコマンドとマッチングする
            if (MatchTTSCommand(logLine))
            {
                // サウンドを鳴らさない
                return false;
            }

            // 通常コマンドとマッチングする
            var match = regexCommand.Match(logLine);
            if (!match.Success)
            {
                return false;
            }

            var r = false;

            var command = match.Groups["command"].ToString().ToLower();
            var target = match.Groups["target"].ToString().ToLower();
            var windowname = match.Groups["windowname"].ToString().Replace(@"""", string.Empty);
            var valueAsText = match.Groups["value"].ToString();
            if (!bool.TryParse(valueAsText, out bool value))
            {
                value = false;
            }

            switch (command)
            {
                case "refresh":
                    switch (target)
                    {
                        case "all":
                            TableCompiler.Instance.RefreshCombatants();
                            TableCompiler.Instance.RefreshPlayerPlacceholder();
                            TableCompiler.Instance.RefreshPartyPlaceholders();
                            TableCompiler.Instance.RefreshPetPlaceholder();
                            TableCompiler.Instance.RecompileSpells();
                            TableCompiler.Instance.RecompileTickers();
                            r = true;
                            break;

                        case "spells":
                            SpellsController.Instance.ClosePanels();
                            r = true;
                            break;

                        case "telops":
                            TickersController.Instance.CloseTelops();
                            r = true;
                            break;

                        case "pt":
                            TableCompiler.Instance.RefreshPlayerPlacceholder();
                            TableCompiler.Instance.RefreshPartyPlaceholders();
                            TableCompiler.Instance.RecompileSpells();
                            TableCompiler.Instance.RecompileTickers();
                            r = true;
                            break;

                        case "pet":
                            TableCompiler.Instance.RefreshPetPlaceholder();
                            TableCompiler.Instance.RecompileSpells();
                            TableCompiler.Instance.RecompileTickers();
                            r = true;
                            break;
                    }

                    break;

                case "changeenabled":
                    switch (target)
                    {
                        case "spells":
                            foreach (var spell in SpellTable.Instance.Table)
                            {
                                if (spell.Panel.PanelName.Trim().ToLower() == windowname.Trim().ToLower() ||
                                    spell.SpellTitle.Trim().ToLower() == windowname.Trim().ToLower() ||
                                    windowname.Trim().ToLower() == "all")
                                {
                                    spell.Enabled = value;
                                    TableCompiler.Instance.RecompileSpells();

                                    r = true;
                                }
                            }

                            break;

                        case "telops":
                            foreach (var telop in TickerTable.Instance.Table)
                            {
                                if (telop.Title.Trim().ToLower() == windowname.Trim().ToLower() ||
                                    windowname.Trim().ToLower() == "all")
                                {
                                    telop.Enabled = value;
                                    TableCompiler.Instance.RecompileTickers();

                                    r = true;
                                }
                            }

                            break;
                    }

                    break;

                case "set":
                    switch (target)
                    {
                        case "placeholder":
                            if (windowname.Trim().ToLower() != "all" &&
                                windowname.Trim() != string.Empty &&
                                valueAsText.Trim() != string.Empty)
                            {
                                TableCompiler.Instance.SetCustomPlaceholder(windowname.Trim(), valueAsText.Trim());

                                r = true;
                            }

                            break;
                    }

                    break;

                case "clear":
                    switch (target)
                    {
                        case "placeholder":
                            if (windowname.Trim().ToLower() == "all")
                            {
                                TableCompiler.Instance.ClearCustomPlaceholderAll();

                                r = true;
                            }
                            else if (windowname.Trim() != string.Empty)
                            {
                                TableCompiler.Instance.ClearCustomPlaceholder(windowname.Trim());

                                r = true;
                            }

                            break;
                    }

                    break;

                case "on":
                    PluginCore.Instance.ChangeSwitchVisibleButton(true);
                    r = true;
                    break;

                case "off":
                    PluginCore.Instance.ChangeSwitchVisibleButton(false);
                    r = true;
                    break;

                case "pos":
                    LogBuffer.DumpPosition();
                    r = true;
                    break;
            }

            return r;
        }

        /// <summary>
        /// 読み仮名設定コマンドとマッチングする
        /// </summary>
        /// <param name="logLine">ログ1行</param>
        /// <returns>
        /// マッチした？</returns>
        public static bool MatchPhoneticCommand(
            string logLine)
        {
            var r = false;

            var match = phoneticsCommand.Match(logLine);
            if (!match.Success)
            {
                return r;
            }

            var pcName = match.Groups["pcname"].ToString();
            var phonetic = match.Groups["phonetic"].ToString();

            if (!string.IsNullOrEmpty(pcName) &&
                !string.IsNullOrEmpty(phonetic))
            {
                r = true;

                var p = TTSDictionary.Instance.Phonetics.FirstOrDefault(x => x.Name == pcName);
                if (p != null)
                {
                    p.Phonetic = phonetic;
                }
                else
                {
                    TTSDictionary.Instance.Dictionary[pcName] = phonetic;
                }
            }

            return r;
        }

        public static bool MatchLogCommand(
            string logLine)
        {
            var r = false;

            var match = logCommand.Match(logLine);
            if (!match.Success)
            {
                return r;
            }

            var switchValue = match.Groups["switch"].ToString();

            if (switchValue == "on")
            {
                r = true;
                Settings.Default.SaveLogEnabled = true;
            }

            if (switchValue == "off")
            {
                r = true;
                Settings.Default.SaveLogEnabled = false;
            }

            if (switchValue == "open")
            {
                r = true;
                var file = ChatLogWorker.Instance.OutputFile;
                if (File.Exists(file))
                {
                    Process.Start(file);
                }
            }

            if (switchValue == "flush")
            {
                r = true;
                ChatLogWorker.Instance.Write(true);
            }

            return r;
        }

        public static bool MatchTTSCommand(
            string logLine)
        {
            var match = ttsCommand.Match(logLine);
            if (!match.Success)
            {
                return false;
            }

            var text = match.Groups["text"].ToString().Trim();
            if (!string.IsNullOrEmpty(text))
            {
                SoundController.Instance.Play(text);
            }

            return true;
        }

        private static bool MatchOptionCommand(
            string logLine)
        {
            var match = lazyOptionCommand.Value.Match(logLine);
            if (!match.Success)
            {
                return false;
            }

            var option = match.Groups["option"].ToString();
            var command = match.Groups["command"].ToString();

            var value = command switch
            {
                "enabled" => true,
                "on" => true,
                _ => false,
            };

            optionCommands.FirstOrDefault(x => x.keyword == command).change?.Invoke(value);

            return true;
        }
    }
}
