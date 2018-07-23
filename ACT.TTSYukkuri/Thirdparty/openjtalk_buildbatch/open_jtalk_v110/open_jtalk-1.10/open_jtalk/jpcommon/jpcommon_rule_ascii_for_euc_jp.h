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

#ifndef JPCOMMON_RULE_H
#define JPCOMMON_RULE_H

#ifdef __cplusplus
#define JPCOMMON_RULE_H_START extern "C" {
#define JPCOMMON_RULE_H_END   }
#else
#define JPCOMMON_RULE_H_START
#define JPCOMMON_RULE_H_END
#endif                          /* __CPLUSPLUS */

JPCOMMON_RULE_H_START;

#define JPCOMMON_MORA_UNVOICE "\xa1\xc7"
#define JPCOMMON_MORA_LONG_VOWEL "\xa1\xbc"
#define JPCOMMON_MORA_SHORT_PAUSE "\xa1\xa2"
#define JPCOMMON_MORA_QUESTION "\xa1\xa9"
#define JPCOMMON_PHONEME_SHORT_PAUSE "pau"
#define JPCOMMON_PHONEME_SILENT "sil"
#define JPCOMMON_PHONEME_UNKNOWN "xx"
#define JPCOMMON_FLAG_QUESTION "1"

static const char *jpcommon_unvoice_list[] = {
   "a", "A",
   "i", "I",
   "u", "U",
   "e", "E",
   "o", "O",
   NULL, NULL
};

static const char *jpcommon_mora_list[] = {
   "\xa5\xf4\xa5\xe7", "by", "o",
   "\xa5\xf4\xa5\xe5", "by", "u",
   "\xa5\xf4\xa5\xe3", "by", "a",
   "\xa5\xf4\xa5\xa9", "v", "o",
   "\xa5\xf4\xa5\xa7", "v", "e",
   "\xa5\xf4\xa5\xa3", "v", "i",
   "\xa5\xf4\xa5\xa1", "v", "a",
   "\xa5\xf4", "v", "u",
   "\xa5\xf3", "N", NULL,
   "\xa5\xf2", "o", NULL,
   "\xa5\xf1", "e", NULL,
   "\xa5\xf0", "i", NULL,
   "\xa5\xef", "w", "a",
   "\xa5\xee", "w", "a",
   "\xa5\xed", "r", "o",
   "\xa5\xec", "r", "e",
   "\xa5\xeb", "r", "u",
   "\xa5\xea\xa5\xe7", "ry", "o",
   "\xa5\xea\xa5\xe5", "ry", "u",
   "\xa5\xea\xa5\xe3", "ry", "a",
   "\xa5\xea\xa5\xa7", "ry", "e",
   "\xa5\xea", "r", "i",
   "\xa5\xe9", "r", "a",
   "\xa5\xe8", "y", "o",
   "\xa5\xe7", "y", "o",
   "\xa5\xe6", "y", "u",
   "\xa5\xe5", "y", "u",
   "\xa5\xe4", "y", "a",
   "\xa5\xe3", "y", "a",
   "\xa5\xe2", "m", "o",
   "\xa5\xe1", "m", "e",
   "\xa5\xe0", "m", "u",
   "\xa5\xdf\xa5\xe7", "my", "o",
   "\xa5\xdf\xa5\xe5", "my", "u",
   "\xa5\xdf\xa5\xe3", "my", "a",
   "\xa5\xdf\xa5\xa7", "my", "e",
   "\xa5\xdf", "m", "i",
   "\xa5\xde", "m", "a",
   "\xa5\xdd", "p", "o",
   "\xa5\xdc", "b", "o",
   "\xa5\xdb", "h", "o",
   "\xa5\xda", "p", "e",
   "\xa5\xd9", "b", "e",
   "\xa5\xd8", "h", "e",
   "\xa5\xd7", "p", "u",
   "\xa5\xd6", "b", "u",
   "\xa5\xd5\xa5\xa9", "f", "o",
   "\xa5\xd5\xa5\xa7", "f", "e",
   "\xa5\xd5\xa5\xa3", "f", "i",
   "\xa5\xd5\xa5\xa1", "f", "a",
   "\xa5\xd5", "f", "u",
   "\xa5\xd4\xa5\xe7", "py", "o",
   "\xa5\xd4\xa5\xe5", "py", "u",
   "\xa5\xd4\xa5\xe3", "py", "a",
   "\xa5\xd4\xa5\xa7", "py", "e",
   "\xa5\xd4", "p", "i",
   "\xa5\xd3\xa5\xe7", "by", "o",
   "\xa5\xd3\xa5\xe5", "by", "u",
   "\xa5\xd3\xa5\xe3", "by", "a",
   "\xa5\xd3\xa5\xa7", "by", "e",
   "\xa5\xd3", "b", "i",
   "\xa5\xd2\xa5\xe7", "hy", "o",
   "\xa5\xd2\xa5\xe5", "hy", "u",
   "\xa5\xd2\xa5\xe3", "hy", "a",
   "\xa5\xd2\xa5\xa7", "hy", "e",
   "\xa5\xd2", "h", "i",
   "\xa5\xd1", "p", "a",
   "\xa5\xd0", "b", "a",
   "\xa5\xcf", "h", "a",
   "\xa5\xce", "n", "o",
   "\xa5\xcd", "n", "e",
   "\xa5\xcc", "n", "u",
   "\xa5\xcb\xa5\xe7", "ny", "o",
   "\xa5\xcb\xa5\xe5", "ny", "u",
   "\xa5\xcb\xa5\xe3", "ny", "a",
   "\xa5\xcb\xa5\xa7", "ny", "e",
   "\xa5\xcb", "n", "i",
   "\xa5\xca", "n", "a",
   "\xa5\xc9\xa5\xa5", "d", "u",
   "\xa5\xc9", "d", "o",
   "\xa5\xc8\xa5\xa5", "t", "u",
   "\xa5\xc8", "t", "o",
   "\xa5\xc7\xa5\xe7", "dy", "o",
   "\xa5\xc7\xa5\xe5", "dy", "u",
   "\xa5\xc7\xa5\xe3", "dy", "a",
   "\xa5\xc7\xa5\xa3", "d", "i",
   "\xa5\xc7", "d", "e",
   "\xa5\xc6\xa5\xe7", "ty", "o",
   "\xa5\xc6\xa5\xe5", "ty", "u",
   "\xa5\xc6\xa5\xe3", "ty", "a",
   "\xa5\xc6\xa5\xa3", "t", "i",
   "\xa5\xc6", "t", "e",
   "\xa5\xc5", "z", "u",
   "\xa5\xc4\xa5\xa9", "ts", "o",
   "\xa5\xc4\xa5\xa7", "ts", "e",
   "\xa5\xc4\xa5\xa3", "ts", "i",
   "\xa5\xc4\xa5\xa1", "ts", "a",
   "\xa5\xc4", "ts", "u",
   "\xa5\xc3", "cl", NULL,
   "\xa5\xc2", "j", "i",
   "\xa5\xc1\xa5\xe7", "ch", "o",
   "\xa5\xc1\xa5\xe5", "ch", "u",
   "\xa5\xc1\xa5\xe3", "ch", "a",
   "\xa5\xc1\xa5\xa7", "ch", "e",
   "\xa5\xc1", "ch", "i",
   "\xa5\xc0", "d", "a",
   "\xa5\xbf", "t", "a",
   "\xa5\xbe", "z", "o",
   "\xa5\xbd", "s", "o",
   "\xa5\xbc", "z", "e",
   "\xa5\xbb", "s", "e",
   "\xa5\xba\xa5\xa3", "z", "i",
   "\xa5\xba", "z", "u",
   "\xa5\xb9\xa5\xa3", "s", "i",
   "\xa5\xb9", "s", "u",
   "\xa5\xb8\xa5\xe7", "j", "o",
   "\xa5\xb8\xa5\xe5", "j", "u",
   "\xa5\xb8\xa5\xe3", "j", "a",
   "\xa5\xb8\xa5\xa7", "j", "e",
   "\xa5\xb8", "j", "i",
   "\xa5\xb7\xa5\xe7", "sh", "o",
   "\xa5\xb7\xa5\xe5", "sh", "u",
   "\xa5\xb7\xa5\xe3", "sh", "a",
   "\xa5\xb7\xa5\xa7", "sh", "e",
   "\xa5\xb7", "sh", "i",
   "\xa5\xb6", "z", "a",
   "\xa5\xb5", "s", "a",
   "\xa5\xb4", "g", "o",
   "\xa5\xb3", "k", "o",
   "\xa5\xb2", "g", "e",
   "\xa5\xb1", "k", "e",
   "\xa5\xf6", "k", "e",
   "\xa5\xb0\xa5\xee", "gw", "a",
   "\xa5\xb0", "g", "u",
   "\xa5\xaf\xa5\xee", "kw", "a",
   "\xa5\xaf", "k", "u",
   "\xa5\xae\xa5\xe7", "gy", "o",
   "\xa5\xae\xa5\xe5", "gy", "u",
   "\xa5\xae\xa5\xe3", "gy", "a",
   "\xa5\xae\xa5\xa7", "gy", "e",
   "\xa5\xae", "g", "i",
   "\xa5\xad\xa5\xe7", "ky", "o",
   "\xa5\xad\xa5\xe5", "ky", "u",
   "\xa5\xad\xa5\xe3", "ky", "a",
   "\xa5\xad\xa5\xa7", "ky", "e",
   "\xa5\xad", "k", "i",
   "\xa5\xac", "g", "a",
   "\xa5\xab", "k", "a",
   "\xa5\xaa", "o", NULL,
   "\xa5\xa9", "o", NULL,
   "\xa5\xa8", "e", NULL,
   "\xa5\xa7", "e", NULL,
   "\xa5\xa6\xa5\xa9", "w", "o",
   "\xa5\xa6\xa5\xa7", "w", "e",
   "\xa5\xa6\xa5\xa3", "w", "i",
   "\xa5\xa6", "u", NULL,
   "\xa5\xa5", "u", NULL,
   "\xa5\xa4\xa5\xa7", "y", "e",
   "\xa5\xa4", "i", NULL,
   "\xa5\xa3", "i", NULL,
   "\xa5\xa2", "a", NULL,
   "\xa5\xa1", "a", NULL,
   NULL, NULL, NULL
};

static const char *jpcommon_pos_list[] = {
   "\xa4\xbd\xa4\xce\xc2\xbe", "xx",
   "\xb4\xb6\xc6\xb0\xbb\xec", "09",
   "\xb5\xad\xb9\xe6", "xx",
   "\xb7\xc1\xbe\xf5\xbb\xec", "19",
   "\xb7\xc1\xcd\xc6\xbb\xec", "01",
   "\xbd\xf5\xbb\xec-\xa4\xbd\xa4\xce\xc2\xbe", "23",
   "\xbd\xf5\xbb\xec-\xb3\xca\xbd\xf5\xbb\xec", "13",
   "\xbd\xf5\xbb\xec-\xb7\xb8\xbd\xf5\xbb\xec", "24",
   "\xbd\xf5\xbb\xec-\xbd\xaa\xbd\xf5\xbb\xec", "14",
   "\xbd\xf5\xbb\xec-\xc0\xdc\xc2\xb3\xbd\xf5\xbb\xec", "12",
   "\xbd\xf5\xbb\xec-\xc9\xfb\xbd\xf5\xbb\xec", "11",
   "\xbd\xf5\xc6\xb0\xbb\xec", "10",
   "\xc0\xdc\xc2\xb3\xbb\xec", "08",
   "\xc0\xdc\xc6\xac\xbc\xad", "16",
   "\xc0\xdc\xc6\xac\xbc\xad-\xb7\xc1\xbe\xf5\xbb\xec\xc5\xaa", "16",
   "\xc0\xdc\xc6\xac\xbc\xad-\xb7\xc1\xcd\xc6\xbb\xec\xc5\xaa", "16",
   "\xc0\xdc\xc6\xac\xbc\xad-\xc6\xb0\xbb\xec\xc5\xaa", "16",
   "\xc0\xdc\xc6\xac\xbc\xad-\xcc\xbe\xbb\xec\xc5\xaa", "16",
   "\xc0\xdc\xc8\xf8\xbc\xad-\xb7\xc1\xbe\xf5\xbb\xec\xc5\xaa", "15",
   "\xc0\xdc\xc8\xf8\xbc\xad-\xb7\xc1\xcd\xc6\xbb\xec\xc5\xaa", "15",
   "\xc0\xdc\xc8\xf8\xbc\xad-\xc6\xb0\xbb\xec\xc5\xaa", "15",
   "\xc0\xdc\xc8\xf8\xbc\xad-\xcc\xbe\xbb\xec\xc5\xaa", "15",
   "\xc2\xe5\xcc\xbe\xbb\xec", "04",
   "\xc6\xb0\xbb\xec", "20",
   "\xc6\xb0\xbb\xec-\xc8\xf3\xbc\xab\xce\xa9", "17",
   "\xc9\xfb\xbb\xec", "06",
   "\xcc\xbe\xbb\xec-\xa5\xb5\xca\xd1\xc0\xdc\xc2\xb3", "03",
   "\xcc\xbe\xbb\xec-\xb8\xc7\xcd\xad\xcc\xbe\xbb\xec", "18",
   "\xcc\xbe\xbb\xec-\xbf\xf4\xbb\xec", "05",
   "\xcc\xbe\xbb\xec-\xc8\xf3\xbc\xab\xce\xa9", "22",
   "\xcc\xbe\xbb\xec-\xc9\xe1\xc4\xcc\xcc\xbe\xbb\xec", "02",
   "\xcf\xa2\xc2\xce\xbb\xec", "07",
   "\xa5\xd5\xa5\xa3\xa5\xe9\xa1\xbc", "25",
   NULL, NULL
};


static const char *jpcommon_cform_list[] = {
   "*", "xx",
   "\xa4\xbd\xa4\xce\xc2\xbe", "6",
   "\xb2\xbe\xc4\xea\xb7\xc1", "4",
   "\xb4\xf0\xcb\xdc\xb7\xc1", "2",
   "\xcc\xa4\xc1\xb3\xb7\xc1", "0",
   "\xcc\xbf\xce\xe1\xb7\xc1", "5",
   "\xcf\xa2\xc2\xce\xb7\xc1", "3",
   "\xcf\xa2\xcd\xd1\xb7\xc1", "1",
   NULL, NULL
};

static const char *jpcommon_ctype_list[] = {
   "*", "xx",
   "\xa5\xab\xb9\xd4\xca\xd1\xb3\xca", "5",
   "\xa5\xb5\xb9\xd4\xca\xd1\xb3\xca", "4",
   "\xa5\xe9\xb9\xd4\xca\xd1\xb3\xca", "6",
   "\xb0\xec\xc3\xca", "3",
   "\xb7\xc1\xcd\xc6\xbb\xec", "7",
   "\xb8\xde\xc3\xca", "1",
   "\xbb\xcd\xc3\xca", "6",
   "\xbd\xf5\xc6\xb0\xbb\xec", "7",
   "\xc6\xf3\xc3\xca", "6",
   "\xc9\xd4\xca\xd1\xb2\xbd", "6",
   "\xca\xb8\xb8\xec\xbd\xf5\xc6\xb0\xbb\xec", "6",
   NULL, NULL
};

JPCOMMON_RULE_H_END;

#endif                          /* !JPCOMMON_RULE_H */
