/* ----------------------------------------------------------------- */
/*           The Japanese TTS System "Open JTalk"                    */
/*           developed by HTS Working Group                          */
/*           http://open-jtalk.sourceforge.net/                      */
/* ----------------------------------------------------------------- */
/*                                                                   */
/*  Copyright (c) 2008-2015  Nagoya Institute of Technology          */
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

#ifndef NJD_SET_UNVOICED_VOWEL_RULE_H
#define NJD_SET_UNVOICED_VOWEL_RULE_H

#ifdef __cplusplus
#define NJD_SET_UNVOICED_VOWEL_RULE_H_START extern "C" {
#define NJD_SET_UNVOICED_VOWEL_RULE_H_END   }
#else
#define NJD_SET_UNVOICED_VOWEL_RULE_H_START
#define NJD_SET_UNVOICED_VOWEL_RULE_H_END
#endif                          /* __CPLUSPLUS */

NJD_SET_UNVOICED_VOWEL_RULE_H_START;

/*
  \xcc\xb5\xc0\xbc\xbb\xd2\xb2\xbb: k ky s sh t ty ch ts h f hy p py
  Rule 0 \xa5\xd5\xa5\xa3\xa5\xe9\xa1\xbc\xa4\xcf\xcc\xb5\xc0\xbc\xb2\xbd\xa4\xb7\xa4\xca\xa4\xa4
  Rule 1 \xbd\xf5\xc6\xb0\xbb\xec\xa4\xce\xa1\xd6\xa4\xc7\xa4\xb9\xa1\xd7\xa4\xc8\xa1\xd6\xa4\xde\xa4\xb9\xa1\xd7\xa4\xce\xa1\xd6\xa4\xb9\xa1\xd7\xa4\xac\xcc\xb5\xc0\xbc\xb2\xbd
  Rule 2 \xc6\xb0\xbb\xec\xa1\xa4\xbd\xf5\xc6\xb0\xbb\xec\xa1\xa4\xbd\xf5\xbb\xec\xa4\xce\xa1\xd6\xa4\xb7\xa1\xd7\xa4\xcf\xcc\xb5\xc0\xbc\xb2\xbd\xa4\xb7\xa4\xe4\xa4\xb9\xa4\xa4
  Rule 3 \xc2\xb3\xa4\xb1\xa4\xc6\xcc\xb5\xc0\xbc\xb2\xbd\xa4\xb7\xa4\xca\xa4\xa4
  Rule 4 \xa5\xa2\xa5\xaf\xa5\xbb\xa5\xf3\xa5\xc8\xb3\xcb\xa4\xc7\xcc\xb5\xc0\xbc\xb2\xbd\xa4\xb7\xa4\xca\xa4\xa4
  Rule 5 \xcc\xb5\xc0\xbc\xbb\xd2\xb2\xbb(k ky s sh t ty ch ts h f hy p py)\xa4\xcb\xb0\xcf\xa4\xde\xa4\xec\xa4\xbf\xa1\xd6i\xa1\xd7\xa4\xc8\xa1\xd6u\xa1\xd7\xa4\xac\xcc\xb5\xc0\xbc\xb2\xbd
         \xce\xe3\xb3\xb0\xa1\xa7s->s, s->sh, f->f, f->h, f->hy, h->f, h->h, h->hy
*/

#define NJD_SET_UNVOICED_VOWEL_FILLER "\xa5\xd5\xa5\xa3\xa5\xe9\xa1\xbc"
#define NJD_SET_UNVOICED_VOWEL_DOUSHI "\xc6\xb0\xbb\xec"
#define NJD_SET_UNVOICED_VOWEL_JODOUSHI "\xbd\xf5\xc6\xb0\xbb\xec"
#define NJD_SET_UNVOICED_VOWEL_JOSHI "\xbd\xf5\xbb\xec"
#define NJD_SET_UNVOICED_VOWEL_KANDOUSHI "\xb4\xb6\xc6\xb0\xbb\xec"
#define NJD_SET_UNVOICED_VOWEL_TOUTEN "\xa1\xa2"
#define NJD_SET_UNVOICED_VOWEL_QUESTION "\xa1\xa9"
#define NJD_SET_UNVOICED_VOWEL_QUOTATION "\xa1\xc7"
#define NJD_SET_UNVOICED_VOWEL_SHI "\xa5\xb7"
#define NJD_SET_UNVOICED_VOWEL_MA "\xa5\xde"
#define NJD_SET_UNVOICED_VOWEL_DE "\xa5\xc7"
#define NJD_SET_UNVOICED_VOWEL_CHOUON "\xa1\xbc"
#define NJD_SET_UNVOICED_VOWEL_SU "\xa5\xb9"

static const char *njd_set_unvoiced_vowel_candidate_list1[] = {
   "\xa5\xb9\xa5\xa3",                    /* s i */
   "\xa5\xb9",                       /* s u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list1[] = {
   "\xa5\xab",                       /* k ky */
   "\xa5\xad",
   "\xa5\xaf",
   "\xa5\xb1",
   "\xa5\xb3",
   "\xa5\xbf",                       /* t ty ch ts */
   "\xa5\xc1",
   "\xa5\xc4",
   "\xa5\xc6",
   "\xa5\xc8",
   "\xa5\xcf",                       /* h f hy */
   "\xa5\xd2",
   "\xa5\xd5",
   "\xa5\xd8",
   "\xa5\xdb",
   "\xa5\xd1",                       /* p py */
   "\xa5\xd4",
   "\xa5\xd7",
   "\xa5\xda",
   "\xa5\xdd",
   NULL
};

static const char *njd_set_unvoiced_vowel_candidate_list2[] = {
   "\xa5\xd5\xa5\xa3",                    /* f i */
   "\xa5\xd2",                       /* h i */
   "\xa5\xd5",                       /* f u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list2[] = {
   "\xa5\xab",                       /* k ky */
   "\xa5\xad",
   "\xa5\xaf",
   "\xa5\xb1",
   "\xa5\xb3",
   "\xa5\xb5",                       /* s sh */
   "\xa5\xb7",
   "\xa5\xb9",
   "\xa5\xbb",
   "\xa5\xbd",
   "\xa5\xbf",                       /* t ty ch ts */
   "\xa5\xc1",
   "\xa5\xc4",
   "\xa5\xc6",
   "\xa5\xc8",
   "\xa5\xd1",                       /* p py */
   "\xa5\xd4",
   "\xa5\xd7",
   "\xa5\xda",
   "\xa5\xdd",
   NULL
};

static const char *njd_set_unvoiced_vowel_candidate_list3[] = {
   "\xa5\xad\xa5\xe5",                    /* ky u */
   "\xa5\xb7\xa5\xe5",                    /* sh u */
   "\xa5\xc1\xa5\xe5",                    /* ch u */
   "\xa5\xc4\xa5\xa3",                    /* ts i */
   "\xa5\xd2\xa5\xe5",                    /* hy u */
   "\xa5\xd4\xa5\xe5",                    /* py u */
   "\xa5\xc6\xa5\xe5",                    /* ty u */
   "\xa5\xc8\xa5\xa5",                    /* t u */
   "\xa5\xc6\xa5\xa3",                    /* t i */
   "\xa5\xad",                       /* k i */
   "\xa5\xaf",                       /* k u */
   "\xa5\xb7",                       /* sh i */
   "\xa5\xc1",                       /* ch i */
   "\xa5\xc4",                       /* ts u */
   "\xa5\xd4",                       /* p i */
   "\xa5\xd7",                       /* p u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list3[] = {
   "\xa5\xab",                       /* k ky */
   "\xa5\xad",
   "\xa5\xaf",
   "\xa5\xb1",
   "\xa5\xb3",
   "\xa5\xb5",                       /* s sh */
   "\xa5\xb7",
   "\xa5\xb9",
   "\xa5\xbb",
   "\xa5\xbd",
   "\xa5\xbf",                       /* t ty ch ts */
   "\xa5\xc1",
   "\xa5\xc4",
   "\xa5\xc6",
   "\xa5\xc8",
   "\xa5\xcf",                       /* h f hy */
   "\xa5\xd2",
   "\xa5\xd5",
   "\xa5\xd8",
   "\xa5\xdb",
   "\xa5\xd1",                       /* p py */
   "\xa5\xd4",
   "\xa5\xd7",
   "\xa5\xda",
   "\xa5\xdd",
   NULL
};

static const char *njd_set_unvoiced_vowel_mora_list[] = {
   "\xa5\xf4\xa5\xe7",
   "\xa5\xf4\xa5\xe5",
   "\xa5\xf4\xa5\xe3",
   "\xa5\xf4\xa5\xa9",
   "\xa5\xf4\xa5\xa7",
   "\xa5\xf4\xa5\xa3",
   "\xa5\xf4\xa5\xa1",
   "\xa5\xf4",
   "\xa5\xf3",
   "\xa5\xf2",
   "\xa5\xf1",
   "\xa5\xf0",
   "\xa5\xef",
   "\xa5\xed",
   "\xa5\xec",
   "\xa5\xeb",
   "\xa5\xea\xa5\xe7",
   "\xa5\xea\xa5\xe5",
   "\xa5\xea\xa5\xe3",
   "\xa5\xea\xa5\xa7",
   "\xa5\xea",
   "\xa5\xe9",
   "\xa5\xe8",
   "\xa5\xe7",
   "\xa5\xe6",
   "\xa5\xe5",
   "\xa5\xe4",
   "\xa5\xe3",
   "\xa5\xe2",
   "\xa5\xe1",
   "\xa5\xe0",
   "\xa5\xdf\xa5\xe7",
   "\xa5\xdf\xa5\xe5",
   "\xa5\xdf\xa5\xe3",
   "\xa5\xdf\xa5\xa7",
   "\xa5\xdf",
   "\xa5\xde",
   "\xa5\xdd",
   "\xa5\xdc",
   "\xa5\xdb",
   "\xa5\xda",
   "\xa5\xd9",
   "\xa5\xd8",
   "\xa5\xd7",
   "\xa5\xd6",
   "\xa5\xd5\xa5\xa9",
   "\xa5\xd5\xa5\xa7",
   "\xa5\xd5\xa5\xa3",
   "\xa5\xd5\xa5\xa1",
   "\xa5\xd5",
   "\xa5\xd4\xa5\xe7",
   "\xa5\xd4\xa5\xe5",
   "\xa5\xd4\xa5\xe3",
   "\xa5\xd4\xa5\xa7",
   "\xa5\xd4",
   "\xa5\xd3\xa5\xe7",
   "\xa5\xd3\xa5\xe5",
   "\xa5\xd3\xa5\xe3",
   "\xa5\xd3\xa5\xa7",
   "\xa5\xd3",
   "\xa5\xd2\xa5\xe7",
   "\xa5\xd2\xa5\xe5",
   "\xa5\xd2\xa5\xe3",
   "\xa5\xd2\xa5\xa7",
   "\xa5\xd2",
   "\xa5\xd1",
   "\xa5\xd0",
   "\xa5\xcf",
   "\xa5\xce",
   "\xa5\xcd",
   "\xa5\xcc",
   "\xa5\xcb\xa5\xe7",
   "\xa5\xcb\xa5\xe5",
   "\xa5\xcb\xa5\xe3",
   "\xa5\xcb\xa5\xa7",
   "\xa5\xcb",
   "\xa5\xca",
   "\xa5\xc9\xa5\xa5",
   "\xa5\xc9",
   "\xa5\xc8\xa5\xa5",
   "\xa5\xc8",
   "\xa5\xc7\xa5\xe7",
   "\xa5\xc7\xa5\xe5",
   "\xa5\xc7\xa5\xe3",
   "\xa5\xc7\xa5\xa3",
   "\xa5\xc7",
   "\xa5\xc6\xa5\xe7",
   "\xa5\xc6\xa5\xe5",
   "\xa5\xc6\xa5\xe3",
   "\xa5\xc6\xa5\xa3",
   "\xa5\xc6",
   "\xa5\xc5",
   "\xa5\xc4\xa5\xa9",
   "\xa5\xc4\xa5\xa7",
   "\xa5\xc4\xa5\xa3",
   "\xa5\xc4\xa5\xa1",
   "\xa5\xc4",
   "\xa5\xc3",
   "\xa5\xc2",
   "\xa5\xc1\xa5\xe7",
   "\xa5\xc1\xa5\xe5",
   "\xa5\xc1\xa5\xe3",
   "\xa5\xc1\xa5\xa7",
   "\xa5\xc1",
   "\xa5\xc0",
   "\xa5\xbf",
   "\xa5\xbe",
   "\xa5\xbd",
   "\xa5\xbc",
   "\xa5\xbb",
   "\xa5\xba\xa5\xa3",
   "\xa5\xba",
   "\xa5\xb9\xa5\xa3",
   "\xa5\xb9",
   "\xa5\xb8\xa5\xe7",
   "\xa5\xb8\xa5\xe5",
   "\xa5\xb8\xa5\xe3",
   "\xa5\xb8\xa5\xa7",
   "\xa5\xb8",
   "\xa5\xb7\xa5\xe7",
   "\xa5\xb7\xa5\xe5",
   "\xa5\xb7\xa5\xe3",
   "\xa5\xb7\xa5\xa7",
   "\xa5\xb7",
   "\xa5\xb6",
   "\xa5\xb5",
   "\xa5\xb4",
   "\xa5\xb3",
   "\xa5\xb2",
   "\xa5\xb1",
   "\xa5\xb0",
   "\xa5\xaf",
   "\xa5\xae\xa5\xe7",
   "\xa5\xae\xa5\xe5",
   "\xa5\xae\xa5\xe3",
   "\xa5\xae\xa5\xa7",
   "\xa5\xae",
   "\xa5\xad\xa5\xe7",
   "\xa5\xad\xa5\xe5",
   "\xa5\xad\xa5\xe3",
   "\xa5\xad\xa5\xa7",
   "\xa5\xad",
   "\xa5\xac",
   "\xa5\xab",
   "\xa5\xaa",
   "\xa5\xa9",
   "\xa5\xa8",
   "\xa5\xa7",
   "\xa5\xa6\xa5\xa9",
   "\xa5\xa6\xa5\xa7",
   "\xa5\xa6\xa5\xa3",
   "\xa5\xa6",
   "\xa5\xa5",
   "\xa5\xa4\xa5\xa7",
   "\xa5\xa4",
   "\xa5\xa3",
   "\xa5\xa2",
   "\xa5\xa1",
   "\xa1\xbc",
   NULL
};

NJD_SET_UNVOICED_VOWEL_RULE_H_END;

#endif                          /* !NJD_SET_UNVOICED_VOWEL_RULE_H */
