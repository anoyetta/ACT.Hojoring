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
   " ", "¡¡",
   "!", "¡ª",
   "\"", "¡É",
   "#", "¡ô",
   "$", "¡ð",
   "%", "¡ó",
   "&", "¡õ",
   "'", "¡Ç",
   "(", "¡Ê",
   ")", "¡Ë",
   "*", "¡ö",
   "+", "¡Ü",
   ",", "¡¤",
   "-", "¡Ý",
   ".", "¡¥",
   "/", "¡¿",
   "0", "£°",
   "1", "£±",
   "2", "£²",
   "3", "£³",
   "4", "£´",
   "5", "£µ",
   "6", "£¶",
   "7", "£·",
   "8", "£¸",
   "9", "£¹",
   ":", "¡§",
   ";", "¡¨",
   "<", "¡ã",
   "=", "¡á",
   ">", "¡ä",
   "?", "¡©",
   "@", "¡÷",
   "A", "£Á",
   "B", "£Â",
   "C", "£Ã",
   "D", "£Ä",
   "E", "£Å",
   "F", "£Æ",
   "G", "£Ç",
   "H", "£È",
   "I", "£É",
   "J", "£Ê",
   "K", "£Ë",
   "L", "£Ì",
   "M", "£Í",
   "N", "£Î",
   "O", "£Ï",
   "P", "£Ð",
   "Q", "£Ñ",
   "R", "£Ò",
   "S", "£Ó",
   "T", "£Ô",
   "U", "£Õ",
   "V", "£Ö",
   "W", "£×",
   "X", "£Ø",
   "Y", "£Ù",
   "Z", "£Ú",
   "[", "¡Î",
   "\\", "¡ï",
   "]", "¡Ï",
   "^", "¡°",
   "_", "¡²",
   "`", "¡Æ",
   "a", "£á",
   "b", "£â",
   "c", "£ã",
   "d", "£ä",
   "e", "£å",
   "f", "£æ",
   "g", "£ç",
   "h", "£è",
   "i", "£é",
   "j", "£ê",
   "k", "£ë",
   "l", "£ì",
   "m", "£í",
   "n", "£î",
   "o", "£ï",
   "p", "£ð",
   "q", "£ñ",
   "r", "£ò",
   "s", "£ó",
   "t", "£ô",
   "u", "£õ",
   "v", "£ö",
   "w", "£÷",
   "x", "£ø",
   "y", "£ù",
   "z", "£ú",
   "{", "¡Ð",
   "|", "¡Ã",
   "}", "¡Ñ",
   "~", "¡Á",
   "Ž³ŽÞ", "¥ô",
   "Ž¶ŽÞ", "¥¬",
   "Ž·ŽÞ", "¥®",
   "Ž¸ŽÞ", "¥°",
   "Ž¹ŽÞ", "¥²",
   "ŽºŽÞ", "¥´",
   "Ž»ŽÞ", "¥¶",
   "Ž¼ŽÞ", "¥¸",
   "Ž½ŽÞ", "¥º",
   "Ž¾ŽÞ", "¥¼",
   "Ž¿ŽÞ", "¥¾",
   "ŽÀŽÞ", "¥À",
   "ŽÁŽÞ", "¥Â",
   "ŽÂŽÞ", "¥Å",
   "ŽÃŽÞ", "¥Ç",
   "ŽÄŽÞ", "¥É",
   "ŽÊŽÞ", "¥Ð",
   "ŽËŽÞ", "¥Ó",
   "ŽÌŽÞ", "¥Ö",
   "ŽÍŽÞ", "¥Ù",
   "ŽÎŽÞ", "¥Ü",
   "ŽÊŽß", "¥Ñ",
   "ŽËŽß", "¥Ô",
   "ŽÌŽß", "¥×",
   "ŽÍŽß", "¥Ú",
   "ŽÎŽß", "¥Ý",
   "Ž¡", "¡£",
   "Ž¢", "¡Ö",
   "Ž£", "¡×",
   "Ž¤", "¡¢",
   "Ž¥", "¡¦",
   "Ž¦", "¥ò",
   "Ž§", "¥¡",
   "Ž¨", "¥£",
   "Ž©", "¥¥",
   "Žª", "¥§",
   "Ž«", "¥©",
   "Ž¬", "¥ã",
   "Ž­", "¥å",
   "Ž®", "¥ç",
   "Ž¯", "¥Ã",
   "Ž°", "¡¼",
   "Ž±", "¥¢",
   "Ž²", "¥¤",
   "Ž³", "¥¦",
   "Ž´", "¥¨",
   "Žµ", "¥ª",
   "Ž¶", "¥«",
   "Ž·", "¥­",
   "Ž¸", "¥¯",
   "Ž¹", "¥±",
   "Žº", "¥³",
   "Ž»", "¥µ",
   "Ž¼", "¥·",
   "Ž½", "¥¹",
   "Ž¾", "¥»",
   "Ž¿", "¥½",
   "ŽÀ", "¥¿",
   "ŽÁ", "¥Á",
   "ŽÂ", "¥Ä",
   "ŽÃ", "¥Æ",
   "ŽÄ", "¥È",
   "ŽÅ", "¥Ê",
   "ŽÆ", "¥Ë",
   "ŽÇ", "¥Ì",
   "ŽÈ", "¥Í",
   "ŽÉ", "¥Î",
   "ŽÊ", "¥Ï",
   "ŽË", "¥Ò",
   "ŽÌ", "¥Õ",
   "ŽÍ", "¥Ø",
   "ŽÎ", "¥Û",
   "ŽÏ", "¥Þ",
   "ŽÐ", "¥ß",
   "ŽÑ", "¥à",
   "ŽÒ", "¥á",
   "ŽÓ", "¥â",
   "ŽÔ", "¥ä",
   "ŽÕ", "¥æ",
   "ŽÖ", "¥è",
   "Ž×", "¥é",
   "ŽØ", "¥ê",
   "ŽÙ", "¥ë",
   "ŽÚ", "¥ì",
   "ŽÛ", "¥í",
   "ŽÜ", "¥ï",
   "ŽÝ", "¥ó",
   "ŽÞ", "",
   "Žß", "",
   NULL, NULL
};

TEXT2MECAB_RULE_H_END;

#endif                          /* !TEXT2MECAB_RULE_H */
