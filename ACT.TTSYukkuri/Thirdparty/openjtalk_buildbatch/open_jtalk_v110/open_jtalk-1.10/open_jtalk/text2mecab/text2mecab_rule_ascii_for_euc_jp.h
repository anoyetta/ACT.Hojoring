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
   " ", "\xa1\xa1",
   "!", "\xa1\xaa",
   "\"", "\xa1\xc9",
   "#", "\xa1\xf4",
   "$", "\xa1\xf0",
   "%", "\xa1\xf3",
   "&", "\xa1\xf5",
   "'", "\xa1\xc7",
   "(", "\xa1\xca",
   ")", "\xa1\xcb",
   "*", "\xa1\xf6",
   "+", "\xa1\xdc",
   ",", "\xa1\xa4",
   "-", "\xa1\xdd",
   ".", "\xa1\xa5",
   "/", "\xa1\xbf",
   "0", "\xa3\xb0",
   "1", "\xa3\xb1",
   "2", "\xa3\xb2",
   "3", "\xa3\xb3",
   "4", "\xa3\xb4",
   "5", "\xa3\xb5",
   "6", "\xa3\xb6",
   "7", "\xa3\xb7",
   "8", "\xa3\xb8",
   "9", "\xa3\xb9",
   ":", "\xa1\xa7",
   ";", "\xa1\xa8",
   "<", "\xa1\xe3",
   "=", "\xa1\xe1",
   ">", "\xa1\xe4",
   "?", "\xa1\xa9",
   "@", "\xa1\xf7",
   "A", "\xa3\xc1",
   "B", "\xa3\xc2",
   "C", "\xa3\xc3",
   "D", "\xa3\xc4",
   "E", "\xa3\xc5",
   "F", "\xa3\xc6",
   "G", "\xa3\xc7",
   "H", "\xa3\xc8",
   "I", "\xa3\xc9",
   "J", "\xa3\xca",
   "K", "\xa3\xcb",
   "L", "\xa3\xcc",
   "M", "\xa3\xcd",
   "N", "\xa3\xce",
   "O", "\xa3\xcf",
   "P", "\xa3\xd0",
   "Q", "\xa3\xd1",
   "R", "\xa3\xd2",
   "S", "\xa3\xd3",
   "T", "\xa3\xd4",
   "U", "\xa3\xd5",
   "V", "\xa3\xd6",
   "W", "\xa3\xd7",
   "X", "\xa3\xd8",
   "Y", "\xa3\xd9",
   "Z", "\xa3\xda",
   "[", "\xa1\xce",
   "\\", "\xa1\xef",
   "]", "\xa1\xcf",
   "^", "\xa1\xb0",
   "_", "\xa1\xb2",
   "`", "\xa1\xc6",
   "a", "\xa3\xe1",
   "b", "\xa3\xe2",
   "c", "\xa3\xe3",
   "d", "\xa3\xe4",
   "e", "\xa3\xe5",
   "f", "\xa3\xe6",
   "g", "\xa3\xe7",
   "h", "\xa3\xe8",
   "i", "\xa3\xe9",
   "j", "\xa3\xea",
   "k", "\xa3\xeb",
   "l", "\xa3\xec",
   "m", "\xa3\xed",
   "n", "\xa3\xee",
   "o", "\xa3\xef",
   "p", "\xa3\xf0",
   "q", "\xa3\xf1",
   "r", "\xa3\xf2",
   "s", "\xa3\xf3",
   "t", "\xa3\xf4",
   "u", "\xa3\xf5",
   "v", "\xa3\xf6",
   "w", "\xa3\xf7",
   "x", "\xa3\xf8",
   "y", "\xa3\xf9",
   "z", "\xa3\xfa",
   "{", "\xa1\xd0",
   "|", "\xa1\xc3",
   "}", "\xa1\xd1",
   "~", "\xa1\xc1",
   "\x8e\xb3\x8e\xde", "\xa5\xf4",
   "\x8e\xb6\x8e\xde", "\xa5\xac",
   "\x8e\xb7\x8e\xde", "\xa5\xae",
   "\x8e\xb8\x8e\xde", "\xa5\xb0",
   "\x8e\xb9\x8e\xde", "\xa5\xb2",
   "\x8e\xba\x8e\xde", "\xa5\xb4",
   "\x8e\xbb\x8e\xde", "\xa5\xb6",
   "\x8e\xbc\x8e\xde", "\xa5\xb8",
   "\x8e\xbd\x8e\xde", "\xa5\xba",
   "\x8e\xbe\x8e\xde", "\xa5\xbc",
   "\x8e\xbf\x8e\xde", "\xa5\xbe",
   "\x8e\xc0\x8e\xde", "\xa5\xc0",
   "\x8e\xc1\x8e\xde", "\xa5\xc2",
   "\x8e\xc2\x8e\xde", "\xa5\xc5",
   "\x8e\xc3\x8e\xde", "\xa5\xc7",
   "\x8e\xc4\x8e\xde", "\xa5\xc9",
   "\x8e\xca\x8e\xde", "\xa5\xd0",
   "\x8e\xcb\x8e\xde", "\xa5\xd3",
   "\x8e\xcc\x8e\xde", "\xa5\xd6",
   "\x8e\xcd\x8e\xde", "\xa5\xd9",
   "\x8e\xce\x8e\xde", "\xa5\xdc",
   "\x8e\xca\x8e\xdf", "\xa5\xd1",
   "\x8e\xcb\x8e\xdf", "\xa5\xd4",
   "\x8e\xcc\x8e\xdf", "\xa5\xd7",
   "\x8e\xcd\x8e\xdf", "\xa5\xda",
   "\x8e\xce\x8e\xdf", "\xa5\xdd",
   "\x8e\xa1", "\xa1\xa3",
   "\x8e\xa2", "\xa1\xd6",
   "\x8e\xa3", "\xa1\xd7",
   "\x8e\xa4", "\xa1\xa2",
   "\x8e\xa5", "\xa1\xa6",
   "\x8e\xa6", "\xa5\xf2",
   "\x8e\xa7", "\xa5\xa1",
   "\x8e\xa8", "\xa5\xa3",
   "\x8e\xa9", "\xa5\xa5",
   "\x8e\xaa", "\xa5\xa7",
   "\x8e\xab", "\xa5\xa9",
   "\x8e\xac", "\xa5\xe3",
   "\x8e\xad", "\xa5\xe5",
   "\x8e\xae", "\xa5\xe7",
   "\x8e\xaf", "\xa5\xc3",
   "\x8e\xb0", "\xa1\xbc",
   "\x8e\xb1", "\xa5\xa2",
   "\x8e\xb2", "\xa5\xa4",
   "\x8e\xb3", "\xa5\xa6",
   "\x8e\xb4", "\xa5\xa8",
   "\x8e\xb5", "\xa5\xaa",
   "\x8e\xb6", "\xa5\xab",
   "\x8e\xb7", "\xa5\xad",
   "\x8e\xb8", "\xa5\xaf",
   "\x8e\xb9", "\xa5\xb1",
   "\x8e\xba", "\xa5\xb3",
   "\x8e\xbb", "\xa5\xb5",
   "\x8e\xbc", "\xa5\xb7",
   "\x8e\xbd", "\xa5\xb9",
   "\x8e\xbe", "\xa5\xbb",
   "\x8e\xbf", "\xa5\xbd",
   "\x8e\xc0", "\xa5\xbf",
   "\x8e\xc1", "\xa5\xc1",
   "\x8e\xc2", "\xa5\xc4",
   "\x8e\xc3", "\xa5\xc6",
   "\x8e\xc4", "\xa5\xc8",
   "\x8e\xc5", "\xa5\xca",
   "\x8e\xc6", "\xa5\xcb",
   "\x8e\xc7", "\xa5\xcc",
   "\x8e\xc8", "\xa5\xcd",
   "\x8e\xc9", "\xa5\xce",
   "\x8e\xca", "\xa5\xcf",
   "\x8e\xcb", "\xa5\xd2",
   "\x8e\xcc", "\xa5\xd5",
   "\x8e\xcd", "\xa5\xd8",
   "\x8e\xce", "\xa5\xdb",
   "\x8e\xcf", "\xa5\xde",
   "\x8e\xd0", "\xa5\xdf",
   "\x8e\xd1", "\xa5\xe0",
   "\x8e\xd2", "\xa5\xe1",
   "\x8e\xd3", "\xa5\xe2",
   "\x8e\xd4", "\xa5\xe4",
   "\x8e\xd5", "\xa5\xe6",
   "\x8e\xd6", "\xa5\xe8",
   "\x8e\xd7", "\xa5\xe9",
   "\x8e\xd8", "\xa5\xea",
   "\x8e\xd9", "\xa5\xeb",
   "\x8e\xda", "\xa5\xec",
   "\x8e\xdb", "\xa5\xed",
   "\x8e\xdc", "\xa5\xef",
   "\x8e\xdd", "\xa5\xf3",
   "\x8e\xde", "",
   "\x8e\xdf", "",
   NULL, NULL
};

TEXT2MECAB_RULE_H_END;

#endif                          /* !TEXT2MECAB_RULE_H */
