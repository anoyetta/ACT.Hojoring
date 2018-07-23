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
   " ", "\x81\x40",
   "!", "\x81\x49",
   "\"", "\x81\x68",
   "#", "\x81\x94",
   "$", "\x81\x90",
   "%", "\x81\x93",
   "&", "\x81\x95",
   "'", "\x81\x66",
   "(", "\x81\x69",
   ")", "\x81\x6a",
   "*", "\x81\x96",
   "+", "\x81\x7b",
   ",", "\x81\x43",
   "-", "\x81\x7c",
   ".", "\x81\x44",
   "/", "\x81\x5e",
   "0", "\x82\x4f",
   "1", "\x82\x50",
   "2", "\x82\x51",
   "3", "\x82\x52",
   "4", "\x82\x53",
   "5", "\x82\x54",
   "6", "\x82\x55",
   "7", "\x82\x56",
   "8", "\x82\x57",
   "9", "\x82\x58",
   ":", "\x81\x46",
   ";", "\x81\x47",
   "<", "\x81\x83",
   "=", "\x81\x81",
   ">", "\x81\x84",
   "?", "\x81\x48",
   "@", "\x81\x97",
   "A", "\x82\x60",
   "B", "\x82\x61",
   "C", "\x82\x62",
   "D", "\x82\x63",
   "E", "\x82\x64",
   "F", "\x82\x65",
   "G", "\x82\x66",
   "H", "\x82\x67",
   "I", "\x82\x68",
   "J", "\x82\x69",
   "K", "\x82\x6a",
   "L", "\x82\x6b",
   "M", "\x82\x6c",
   "N", "\x82\x6d",
   "O", "\x82\x6e",
   "P", "\x82\x6f",
   "Q", "\x82\x70",
   "R", "\x82\x71",
   "S", "\x82\x72",
   "T", "\x82\x73",
   "U", "\x82\x74",
   "V", "\x82\x75",
   "W", "\x82\x76",
   "X", "\x82\x77",
   "Y", "\x82\x78",
   "Z", "\x82\x79",
   "[", "\x81\x6d",
   "\\", "\x81\x8f",
   "]", "\x81\x6e",
   "^", "\x81\x4f",
   "_", "\x81\x51",
   "`", "\x81\x65",
   "a", "\x82\x81",
   "b", "\x82\x82",
   "c", "\x82\x83",
   "d", "\x82\x84",
   "e", "\x82\x85",
   "f", "\x82\x86",
   "g", "\x82\x87",
   "h", "\x82\x88",
   "i", "\x82\x89",
   "j", "\x82\x8a",
   "k", "\x82\x8b",
   "l", "\x82\x8c",
   "m", "\x82\x8d",
   "n", "\x82\x8e",
   "o", "\x82\x8f",
   "p", "\x82\x90",
   "q", "\x82\x91",
   "r", "\x82\x92",
   "s", "\x82\x93",
   "t", "\x82\x94",
   "u", "\x82\x95",
   "v", "\x82\x96",
   "w", "\x82\x97",
   "x", "\x82\x98",
   "y", "\x82\x99",
   "z", "\x82\x9a",
   "{", "\x81\x6f",
   "|", "\x81\x62",
   "}", "\x81\x70",
   "~", "\x81\x60",
   "\xb3\xde", "\x83\x94",
   "\xb6\xde", "\x83\x4b",
   "\xb7\xde", "\x83\x4d",
   "\xb8\xde", "\x83\x4f",
   "\xb9\xde", "\x83\x51",
   "\xba\xde", "\x83\x53",
   "\xbb\xde", "\x83\x55",
   "\xbc\xde", "\x83\x57",
   "\xbd\xde", "\x83\x59",
   "\xbe\xde", "\x83\x5b",
   "\xbf\xde", "\x83\x5d",
   "\xc0\xde", "\x83\x5f",
   "\xc1\xde", "\x83\x61",
   "\xc2\xde", "\x83\x64",
   "\xc3\xde", "\x83\x66",
   "\xc4\xde", "\x83\x68",
   "\xca\xde", "\x83\x6f",
   "\xcb\xde", "\x83\x72",
   "\xcc\xde", "\x83\x75",
   "\xcd\xde", "\x83\x78",
   "\xce\xde", "\x83\x7b",
   "\xca\xdf", "\x83\x70",
   "\xcb\xdf", "\x83\x73",
   "\xcc\xdf", "\x83\x76",
   "\xcd\xdf", "\x83\x79",
   "\xce\xdf", "\x83\x7c",
   "\xa1", "\x81\x42",
   "\xa2", "\x81\x75",
   "\xa3", "\x81\x76",
   "\xa4", "\x81\x41",
   "\xa5", "\x81\x45",
   "\xa6", "\x83\x92",
   "\xa7", "\x83\x40",
   "\xa8", "\x83\x42",
   "\xa9", "\x83\x44",
   "\xaa", "\x83\x46",
   "\xab", "\x83\x48",
   "\xac", "\x83\x83",
   "\xad", "\x83\x85",
   "\xae", "\x83\x87",
   "\xaf", "\x83\x62",
   "\xb0", "\x81\x5b",
   "\xb1", "\x83\x41",
   "\xb2", "\x83\x43",
   "\xb3", "\x83\x45",
   "\xb4", "\x83\x47",
   "\xb5", "\x83\x49",
   "\xb6", "\x83\x4a",
   "\xb7", "\x83\x4c",
   "\xb8", "\x83\x4e",
   "\xb9", "\x83\x50",
   "\xba", "\x83\x52",
   "\xbb", "\x83\x54",
   "\xbc", "\x83\x56",
   "\xbd", "\x83\x58",
   "\xbe", "\x83\x5a",
   "\xbf", "\x83\x5c",
   "\xc0", "\x83\x5e",
   "\xc1", "\x83\x60",
   "\xc2", "\x83\x63",
   "\xc3", "\x83\x65",
   "\xc4", "\x83\x67",
   "\xc5", "\x83\x69",
   "\xc6", "\x83\x6a",
   "\xc7", "\x83\x6b",
   "\xc8", "\x83\x6c",
   "\xc9", "\x83\x6d",
   "\xca", "\x83\x6e",
   "\xcb", "\x83\x71",
   "\xcc", "\x83\x74",
   "\xcd", "\x83\x77",
   "\xce", "\x83\x7a",
   "\xcf", "\x83\x7d",
   "\xd0", "\x83\x7e",
   "\xd1", "\x83\x80",
   "\xd2", "\x83\x81",
   "\xd3", "\x83\x82",
   "\xd4", "\x83\x84",
   "\xd5", "\x83\x86",
   "\xd6", "\x83\x88",
   "\xd7", "\x83\x89",
   "\xd8", "\x83\x8a",
   "\xd9", "\x83\x8b",
   "\xda", "\x83\x8c",
   "\xdb", "\x83\x8d",
   "\xdc", "\x83\x8f",
   "\xdd", "\x83\x93",
   "\xde", "",
   "\xdf", "",
   NULL, NULL
};

TEXT2MECAB_RULE_H_END;

#endif                          /* !TEXT2MECAB_RULE_H */
