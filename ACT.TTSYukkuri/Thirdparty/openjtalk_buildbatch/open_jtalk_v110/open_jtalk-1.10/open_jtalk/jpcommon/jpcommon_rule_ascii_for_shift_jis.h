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

#define JPCOMMON_MORA_UNVOICE "\x81\x66"
#define JPCOMMON_MORA_LONG_VOWEL "\x81\x5b"
#define JPCOMMON_MORA_SHORT_PAUSE "\x81\x41"
#define JPCOMMON_MORA_QUESTION "\x81\x48"
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
   "\x83\x94\x83\x87", "by", "o",
   "\x83\x94\x83\x85", "by", "u",
   "\x83\x94\x83\x83", "by", "a",
   "\x83\x94\x83\x48", "v", "o",
   "\x83\x94\x83\x46", "v", "e",
   "\x83\x94\x83\x42", "v", "i",
   "\x83\x94\x83\x40", "v", "a",
   "\x83\x94", "v", "u",
   "\x83\x93", "N", NULL,
   "\x83\x92", "o", NULL,
   "\x83\x91", "e", NULL,
   "\x83\x90", "i", NULL,
   "\x83\x8f", "w", "a",
   "\x83\x8e", "w", "a",
   "\x83\x8d", "r", "o",
   "\x83\x8c", "r", "e",
   "\x83\x8b", "r", "u",
   "\x83\x8a\x83\x87", "ry", "o",
   "\x83\x8a\x83\x85", "ry", "u",
   "\x83\x8a\x83\x83", "ry", "a",
   "\x83\x8a\x83\x46", "ry", "e",
   "\x83\x8a", "r", "i",
   "\x83\x89", "r", "a",
   "\x83\x88", "y", "o",
   "\x83\x87", "y", "o",
   "\x83\x86", "y", "u",
   "\x83\x85", "y", "u",
   "\x83\x84", "y", "a",
   "\x83\x83", "y", "a",
   "\x83\x82", "m", "o",
   "\x83\x81", "m", "e",
   "\x83\x80", "m", "u",
   "\x83\x7e\x83\x87", "my", "o",
   "\x83\x7e\x83\x85", "my", "u",
   "\x83\x7e\x83\x83", "my", "a",
   "\x83\x7e\x83\x46", "my", "e",
   "\x83\x7e", "m", "i",
   "\x83\x7d", "m", "a",
   "\x83\x7c", "p", "o",
   "\x83\x7b", "b", "o",
   "\x83\x7a", "h", "o",
   "\x83\x79", "p", "e",
   "\x83\x78", "b", "e",
   "\x83\x77", "h", "e",
   "\x83\x76", "p", "u",
   "\x83\x75", "b", "u",
   "\x83\x74\x83\x48", "f", "o",
   "\x83\x74\x83\x46", "f", "e",
   "\x83\x74\x83\x42", "f", "i",
   "\x83\x74\x83\x40", "f", "a",
   "\x83\x74", "f", "u",
   "\x83\x73\x83\x87", "py", "o",
   "\x83\x73\x83\x85", "py", "u",
   "\x83\x73\x83\x83", "py", "a",
   "\x83\x73\x83\x46", "py", "e",
   "\x83\x73", "p", "i",
   "\x83\x72\x83\x87", "by", "o",
   "\x83\x72\x83\x85", "by", "u",
   "\x83\x72\x83\x83", "by", "a",
   "\x83\x72\x83\x46", "by", "e",
   "\x83\x72", "b", "i",
   "\x83\x71\x83\x87", "hy", "o",
   "\x83\x71\x83\x85", "hy", "u",
   "\x83\x71\x83\x83", "hy", "a",
   "\x83\x71\x83\x46", "hy", "e",
   "\x83\x71", "h", "i",
   "\x83\x70", "p", "a",
   "\x83\x6f", "b", "a",
   "\x83\x6e", "h", "a",
   "\x83\x6d", "n", "o",
   "\x83\x6c", "n", "e",
   "\x83\x6b", "n", "u",
   "\x83\x6a\x83\x87", "ny", "o",
   "\x83\x6a\x83\x85", "ny", "u",
   "\x83\x6a\x83\x83", "ny", "a",
   "\x83\x6a\x83\x46", "ny", "e",
   "\x83\x6a", "n", "i",
   "\x83\x69", "n", "a",
   "\x83\x68\x83\x44", "d", "u",
   "\x83\x68", "d", "o",
   "\x83\x67\x83\x44", "t", "u",
   "\x83\x67", "t", "o",
   "\x83\x66\x83\x87", "dy", "o",
   "\x83\x66\x83\x85", "dy", "u",
   "\x83\x66\x83\x83", "dy", "a",
   "\x83\x66\x83\x42", "d", "i",
   "\x83\x66", "d", "e",
   "\x83\x65\x83\x87", "ty", "o",
   "\x83\x65\x83\x85", "ty", "u",
   "\x83\x65\x83\x83", "ty", "a",
   "\x83\x65\x83\x42", "t", "i",
   "\x83\x65", "t", "e",
   "\x83\x64", "z", "u",
   "\x83\x63\x83\x48", "ts", "o",
   "\x83\x63\x83\x46", "ts", "e",
   "\x83\x63\x83\x42", "ts", "i",
   "\x83\x63\x83\x40", "ts", "a",
   "\x83\x63", "ts", "u",
   "\x83\x62", "cl", NULL,
   "\x83\x61", "j", "i",
   "\x83\x60\x83\x87", "ch", "o",
   "\x83\x60\x83\x85", "ch", "u",
   "\x83\x60\x83\x83", "ch", "a",
   "\x83\x60\x83\x46", "ch", "e",
   "\x83\x60", "ch", "i",
   "\x83\x5f", "d", "a",
   "\x83\x5e", "t", "a",
   "\x83\x5d", "z", "o",
   "\x83\x5c", "s", "o",
   "\x83\x5b", "z", "e",
   "\x83\x5a", "s", "e",
   "\x83\x59\x83\x42", "z", "i",
   "\x83\x59", "z", "u",
   "\x83\x58\x83\x42", "s", "i",
   "\x83\x58", "s", "u",
   "\x83\x57\x83\x87", "j", "o",
   "\x83\x57\x83\x85", "j", "u",
   "\x83\x57\x83\x83", "j", "a",
   "\x83\x57\x83\x46", "j", "e",
   "\x83\x57", "j", "i",
   "\x83\x56\x83\x87", "sh", "o",
   "\x83\x56\x83\x85", "sh", "u",
   "\x83\x56\x83\x83", "sh", "a",
   "\x83\x56\x83\x46", "sh", "e",
   "\x83\x56", "sh", "i",
   "\x83\x55", "z", "a",
   "\x83\x54", "s", "a",
   "\x83\x53", "g", "o",
   "\x83\x52", "k", "o",
   "\x83\x51", "g", "e",
   "\x83\x50", "k", "e",
   "\x83\x96", "k", "e",
   "\x83\x4f\x83\x8e", "gw", "a",
   "\x83\x4f", "g", "u",
   "\x83\x4e\x83\x8e", "kw", "a",
   "\x83\x4e", "k", "u",
   "\x83\x4d\x83\x87", "gy", "o",
   "\x83\x4d\x83\x85", "gy", "u",
   "\x83\x4d\x83\x83", "gy", "a",
   "\x83\x4d\x83\x46", "gy", "e",
   "\x83\x4d", "g", "i",
   "\x83\x4c\x83\x87", "ky", "o",
   "\x83\x4c\x83\x85", "ky", "u",
   "\x83\x4c\x83\x83", "ky", "a",
   "\x83\x4c\x83\x46", "ky", "e",
   "\x83\x4c", "k", "i",
   "\x83\x4b", "g", "a",
   "\x83\x4a", "k", "a",
   "\x83\x49", "o", NULL,
   "\x83\x48", "o", NULL,
   "\x83\x47", "e", NULL,
   "\x83\x46", "e", NULL,
   "\x83\x45\x83\x48", "w", "o",
   "\x83\x45\x83\x46", "w", "e",
   "\x83\x45\x83\x42", "w", "i",
   "\x83\x45", "u", NULL,
   "\x83\x44", "u", NULL,
   "\x83\x43\x83\x46", "y", "e",
   "\x83\x43", "i", NULL,
   "\x83\x42", "i", NULL,
   "\x83\x41", "a", NULL,
   "\x83\x40", "a", NULL,
   NULL, NULL, NULL
};

static const char *jpcommon_pos_list[] = {
   "\x82\xbb\x82\xcc\x91\xbc", "xx",
   "\x8a\xb4\x93\xae\x8e\x8c", "09",
   "\x8b\x4c\x8d\x86", "xx",
   "\x8c\x60\x8f\xf3\x8e\x8c", "19",
   "\x8c\x60\x97\x65\x8e\x8c", "01",
   "\x8f\x95\x8e\x8c-\x82\xbb\x82\xcc\x91\xbc", "23",
   "\x8f\x95\x8e\x8c-\x8a\x69\x8f\x95\x8e\x8c", "13",
   "\x8f\x95\x8e\x8c-\x8c\x57\x8f\x95\x8e\x8c", "24",
   "\x8f\x95\x8e\x8c-\x8f\x49\x8f\x95\x8e\x8c", "14",
   "\x8f\x95\x8e\x8c-\x90\xda\x91\xb1\x8f\x95\x8e\x8c", "12",
   "\x8f\x95\x8e\x8c-\x95\x9b\x8f\x95\x8e\x8c", "11",
   "\x8f\x95\x93\xae\x8e\x8c", "10",
   "\x90\xda\x91\xb1\x8e\x8c", "08",
   "\x90\xda\x93\xaa\x8e\xab", "16",
   "\x90\xda\x93\xaa\x8e\xab-\x8c\x60\x8f\xf3\x8e\x8c\x93\x49", "16",
   "\x90\xda\x93\xaa\x8e\xab-\x8c\x60\x97\x65\x8e\x8c\x93\x49", "16",
   "\x90\xda\x93\xaa\x8e\xab-\x93\xae\x8e\x8c\x93\x49", "16",
   "\x90\xda\x93\xaa\x8e\xab-\x96\xbc\x8e\x8c\x93\x49", "16",
   "\x90\xda\x94\xf6\x8e\xab-\x8c\x60\x8f\xf3\x8e\x8c\x93\x49", "15",
   "\x90\xda\x94\xf6\x8e\xab-\x8c\x60\x97\x65\x8e\x8c\x93\x49", "15",
   "\x90\xda\x94\xf6\x8e\xab-\x93\xae\x8e\x8c\x93\x49", "15",
   "\x90\xda\x94\xf6\x8e\xab-\x96\xbc\x8e\x8c\x93\x49", "15",
   "\x91\xe3\x96\xbc\x8e\x8c", "04",
   "\x93\xae\x8e\x8c", "20",
   "\x93\xae\x8e\x8c-\x94\xf1\x8e\xa9\x97\xa7", "17",
   "\x95\x9b\x8e\x8c", "06",
   "\x96\xbc\x8e\x8c-\x83\x54\x95\xcf\x90\xda\x91\xb1", "03",
   "\x96\xbc\x8e\x8c-\x8c\xc5\x97\x4c\x96\xbc\x8e\x8c", "18",
   "\x96\xbc\x8e\x8c-\x90\x94\x8e\x8c", "05",
   "\x96\xbc\x8e\x8c-\x94\xf1\x8e\xa9\x97\xa7", "22",
   "\x96\xbc\x8e\x8c-\x95\x81\x92\xca\x96\xbc\x8e\x8c", "02",
   "\x98\x41\x91\xcc\x8e\x8c", "07",
   "\x83\x74\x83\x42\x83\x89\x81\x5b", "25",
   NULL, NULL
};


static const char *jpcommon_cform_list[] = {
   "*", "xx",
   "\x82\xbb\x82\xcc\x91\xbc", "6",
   "\x89\xbc\x92\xe8\x8c\x60", "4",
   "\x8a\xee\x96\x7b\x8c\x60", "2",
   "\x96\xa2\x91\x52\x8c\x60", "0",
   "\x96\xbd\x97\xdf\x8c\x60", "5",
   "\x98\x41\x91\xcc\x8c\x60", "3",
   "\x98\x41\x97\x70\x8c\x60", "1",
   NULL, NULL
};

static const char *jpcommon_ctype_list[] = {
   "*", "xx",
   "\x83\x4a\x8d\x73\x95\xcf\x8a\x69", "5",
   "\x83\x54\x8d\x73\x95\xcf\x8a\x69", "4",
   "\x83\x89\x8d\x73\x95\xcf\x8a\x69", "6",
   "\x88\xea\x92\x69", "3",
   "\x8c\x60\x97\x65\x8e\x8c", "7",
   "\x8c\xdc\x92\x69", "1",
   "\x8e\x6c\x92\x69", "6",
   "\x8f\x95\x93\xae\x8e\x8c", "7",
   "\x93\xf1\x92\x69", "6",
   "\x95\x73\x95\xcf\x89\xbb", "6",
   "\x95\xb6\x8c\xea\x8f\x95\x93\xae\x8e\x8c", "6",
   NULL, NULL
};

JPCOMMON_RULE_H_END;

#endif                          /* !JPCOMMON_RULE_H */
