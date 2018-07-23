/* ----------------------------------------------------------------- */
/*           The Japanese TTS System "Open JTalk"                    */
/*           developed by HTS Working Group                          */
/*           http://open-jtalk.sourceforge.net/                      */
/* ----------------------------------------------------------------- */
/*                                                                   */
/*  Copyright (c) 2008-2016  Nagoya Institute of Technology          */
/*                           Department of Computer Science          */
/*                                                                   */
/* All rights reserved.                                              */
/*                                                                   */
/* Redistribution and use in source and binary forms, with or        */
/* without modification, are permitted provided that the following   */
/* conditions are met:                                               */
/*                                                                   */
/* - Redistributions of source code must retain the above copyright  */
/*   notice, this list of conditions and the following disclaimer.   */
/* - Redistributions in binary form must reproduce the above         */
/*   copyright notice, this list of conditions and the following     */
/*   disclaimer in the documentation and/or other materials provided */
/*   with the distribution.                                          */
/* - Neither the name of the HTS working group nor the names of its  */
/*   contributors may be used to endorse or promote products derived */
/*   from this software without specific prior written permission.   */
/*                                                                   */
/* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND            */
/* CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,       */
/* INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF          */
/* MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE          */
/* DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS */
/* BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,          */
/* EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED   */
/* TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,     */
/* DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON */
/* ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,   */
/* OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY    */
/* OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE           */
/* POSSIBILITY OF SUCH DAMAGE.                                       */
/* ----------------------------------------------------------------- */

#ifndef TEXT2MECAB_RULE_H
#define TEXT2MECAB_RULE_H

#ifdef __cplusplus
#define TEXT2MECAB_RULE_H_START extern "C" {
#define TEXT2MECAB_RULE_H_END   }
#else
#define TEXT2MECAB_RULE_H_START
#define TEXT2MECAB_RULE_H_END
#endif                          /* __CPLUSPLUS */

TEXT2MECAB_RULE_H_START;

static const char text2mecab_control_range[] = {
   0x00, 0x7F
};

static const char text2mecab_kanji_range[] = {
#ifdef CHARSET_EUC_JP
   2, 0xA1, 0xFE,
   3, 0x8F, 0x8F,
#endif                          /* CHARSET_EUC_JP */
#ifdef CHARSET_SHIFT_JIS
   2, 0x81, 0xFC,
#endif                          /* CHARSET_SHIFT_JIS */
#ifdef CHARSET_UTF_8
   2, 0xC0, 0xDF,
   3, 0xE0, 0xEF,
   4, 0xF0, 0xF7,
#endif                          /* CHARSET_UTF_8 */
   -1, -1, -1
};

static const char *text2mecab_conv_list[] = {
   " ", "Å@",
   "!", "ÅI",
   "\"", "Åh",
   "#", "Åî",
   "$", "Åê",
   "%", "Åì",
   "&", "Åï",
   "'", "Åf",
   "(", "Åi",
   ")", "Åj",
   "*", "Åñ",
   "+", "Å{",
   ",", "ÅC",
   "-", "Å|",
   ".", "ÅD",
   "/", "Å^",
   "0", "ÇO",
   "1", "ÇP",
   "2", "ÇQ",
   "3", "ÇR",
   "4", "ÇS",
   "5", "ÇT",
   "6", "ÇU",
   "7", "ÇV",
   "8", "ÇW",
   "9", "ÇX",
   ":", "ÅF",
   ";", "ÅG",
   "<", "ÅÉ",
   "=", "ÅÅ",
   ">", "ÅÑ",
   "?", "ÅH",
   "@", "Åó",
   "A", "Ç`",
   "B", "Ça",
   "C", "Çb",
   "D", "Çc",
   "E", "Çd",
   "F", "Çe",
   "G", "Çf",
   "H", "Çg",
   "I", "Çh",
   "J", "Çi",
   "K", "Çj",
   "L", "Çk",
   "M", "Çl",
   "N", "Çm",
   "O", "Çn",
   "P", "Ço",
   "Q", "Çp",
   "R", "Çq",
   "S", "Çr",
   "T", "Çs",
   "U", "Çt",
   "V", "Çu",
   "W", "Çv",
   "X", "Çw",
   "Y", "Çx",
   "Z", "Çy",
   "[", "Åm",
   "\\", "Åè",
   "]", "Ån",
   "^", "ÅO",
   "_", "ÅQ",
   "`", "Åe",
   "a", "ÇÅ",
   "b", "ÇÇ",
   "c", "ÇÉ",
   "d", "ÇÑ",
   "e", "ÇÖ",
   "f", "ÇÜ",
   "g", "Çá",
   "h", "Çà",
   "i", "Çâ",
   "j", "Çä",
   "k", "Çã",
   "l", "Çå",
   "m", "Çç",
   "n", "Çé",
   "o", "Çè",
   "p", "Çê",
   "q", "Çë",
   "r", "Çí",
   "s", "Çì",
   "t", "Çî",
   "u", "Çï",
   "v", "Çñ",
   "w", "Çó",
   "x", "Çò",
   "y", "Çô",
   "z", "Çö",
   "{", "Åo",
   "|", "Åb",
   "}", "Åp",
   "~", "Å`",
   "≥ﬁ", "Éî",
   "∂ﬁ", "ÉK",
   "∑ﬁ", "ÉM",
   "∏ﬁ", "ÉO",
   "πﬁ", "ÉQ",
   "∫ﬁ", "ÉS",
   "ªﬁ", "ÉU",
   "ºﬁ", "ÉW",
   "Ωﬁ", "ÉY",
   "æﬁ", "É[",
   "øﬁ", "É]",
   "¿ﬁ", "É_",
   "¡ﬁ", "Éa",
   "¬ﬁ", "Éd",
   "√ﬁ", "Éf",
   "ƒﬁ", "Éh",
   " ﬁ", "Éo",
   "Àﬁ", "Ér",
   "Ãﬁ", "Éu",
   "Õﬁ", "Éx",
   "Œﬁ", "É{",
   " ﬂ", "Ép",
   "Àﬂ", "És",
   "Ãﬂ", "Év",
   "Õﬂ", "Éy",
   "Œﬂ", "É|",
   "°", "ÅB",
   "¢", "Åu",
   "£", "Åv",
   "§", "ÅA",
   "•", "ÅE",
   "¶", "Éí",
   "ß", "É@",
   "®", "ÉB",
   "©", "ÉD",
   "™", "ÉF",
   "´", "ÉH",
   "¨", "ÉÉ",
   "≠", "ÉÖ",
   "Æ", "Éá",
   "Ø", "Éb",
   "∞", "Å[",
   "±", "ÉA",
   "≤", "ÉC",
   "≥", "ÉE",
   "¥", "ÉG",
   "µ", "ÉI",
   "∂", "ÉJ",
   "∑", "ÉL",
   "∏", "ÉN",
   "π", "ÉP",
   "∫", "ÉR",
   "ª", "ÉT",
   "º", "ÉV",
   "Ω", "ÉX",
   "æ", "ÉZ",
   "ø", "É\",
   "¿", "É^",
   "¡", "É`",
   "¬", "Éc",
   "√", "Ée",
   "ƒ", "Ég",
   "≈", "Éi",
   "∆", "Éj",
   "«", "Ék",
   "»", "Él",
   "…", "Ém",
   " ", "Én",
   "À", "Éq",
   "Ã", "Ét",
   "Õ", "Éw",
   "Œ", "Éz",
   "œ", "É}",
   "–", "É~",
   "—", "ÉÄ",
   "“", "ÉÅ",
   "”", "ÉÇ",
   "‘", "ÉÑ",
   "’", "ÉÜ",
   "÷", "Éà",
   "◊", "Éâ",
   "ÿ", "Éä",
   "Ÿ", "Éã",
   "⁄", "Éå",
   "€", "Éç",
   "‹", "Éè",
   "›", "Éì",
   "ﬁ", "",
   "ﬂ", "",
   NULL, NULL
};

TEXT2MECAB_RULE_H_END;

#endif                          /* !TEXT2MECAB_RULE_H */
