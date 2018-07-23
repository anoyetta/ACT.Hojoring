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
  \x96\xb3\x90\xba\x8e\x71\x89\xb9: k ky s sh t ty ch ts h f hy p py
  Rule 0 \x83\x74\x83\x42\x83\x89\x81\x5b\x82\xcd\x96\xb3\x90\xba\x89\xbb\x82\xb5\x82\xc8\x82\xa2
  Rule 1 \x8f\x95\x93\xae\x8e\x8c\x82\xcc\x81\x75\x82\xc5\x82\xb7\x81\x76\x82\xc6\x81\x75\x82\xdc\x82\xb7\x81\x76\x82\xcc\x81\x75\x82\xb7\x81\x76\x82\xaa\x96\xb3\x90\xba\x89\xbb
  Rule 2 \x93\xae\x8e\x8c\x81\x43\x8f\x95\x93\xae\x8e\x8c\x81\x43\x8f\x95\x8e\x8c\x82\xcc\x81\x75\x82\xb5\x81\x76\x82\xcd\x96\xb3\x90\xba\x89\xbb\x82\xb5\x82\xe2\x82\xb7\x82\xa2
  Rule 3 \x91\xb1\x82\xaf\x82\xc4\x96\xb3\x90\xba\x89\xbb\x82\xb5\x82\xc8\x82\xa2
  Rule 4 \x83\x41\x83\x4e\x83\x5a\x83\x93\x83\x67\x8a\x6a\x82\xc5\x96\xb3\x90\xba\x89\xbb\x82\xb5\x82\xc8\x82\xa2
  Rule 5 \x96\xb3\x90\xba\x8e\x71\x89\xb9(k ky s sh t ty ch ts h f hy p py)\x82\xc9\x88\xcd\x82\xdc\x82\xea\x82\xbd\x81\x75i\x81\x76\x82\xc6\x81\x75u\x81\x76\x82\xaa\x96\xb3\x90\xba\x89\xbb
         \x97\xe1\x8a\x4f\x81\x46s->s, s->sh, f->f, f->h, f->hy, h->f, h->h, h->hy
*/

#define NJD_SET_UNVOICED_VOWEL_FILLER "\x83\x74\x83\x42\x83\x89\x81\x5b"
#define NJD_SET_UNVOICED_VOWEL_DOUSHI "\x93\xae\x8e\x8c"
#define NJD_SET_UNVOICED_VOWEL_JODOUSHI "\x8f\x95\x93\xae\x8e\x8c"
#define NJD_SET_UNVOICED_VOWEL_JOSHI "\x8f\x95\x8e\x8c"
#define NJD_SET_UNVOICED_VOWEL_KANDOUSHI "\x8a\xb4\x93\xae\x8e\x8c"
#define NJD_SET_UNVOICED_VOWEL_TOUTEN "\x81\x41"
#define NJD_SET_UNVOICED_VOWEL_QUESTION "\x81\x48"
#define NJD_SET_UNVOICED_VOWEL_QUOTATION "\x81\x66"
#define NJD_SET_UNVOICED_VOWEL_SHI "\x83\x56"
#define NJD_SET_UNVOICED_VOWEL_MA "\x83\x7d"
#define NJD_SET_UNVOICED_VOWEL_DE "\x83\x66"
#define NJD_SET_UNVOICED_VOWEL_CHOUON "\x81\x5b"
#define NJD_SET_UNVOICED_VOWEL_SU "\x83\x58"

static const char *njd_set_unvoiced_vowel_candidate_list1[] = {
   "\x83\x58\x83\x42",                    /* s i */
   "\x83\x58",                       /* s u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list1[] = {
   "\x83\x4a",                       /* k ky */
   "\x83\x4c",
   "\x83\x4e",
   "\x83\x50",
   "\x83\x52",
   "\x83\x5e",                       /* t ty ch ts */
   "\x83\x60",
   "\x83\x63",
   "\x83\x65",
   "\x83\x67",
   "\x83\x6e",                       /* h f hy */
   "\x83\x71",
   "\x83\x74",
   "\x83\x77",
   "\x83\x7a",
   "\x83\x70",                       /* p py */
   "\x83\x73",
   "\x83\x76",
   "\x83\x79",
   "\x83\x7c",
   NULL
};

static const char *njd_set_unvoiced_vowel_candidate_list2[] = {
   "\x83\x74\x83\x42",                    /* f i */
   "\x83\x71",                       /* h i */
   "\x83\x74",                       /* f u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list2[] = {
   "\x83\x4a",                       /* k ky */
   "\x83\x4c",
   "\x83\x4e",
   "\x83\x50",
   "\x83\x52",
   "\x83\x54",                       /* s sh */
   "\x83\x56",
   "\x83\x58",
   "\x83\x5a",
   "\x83\x5c",
   "\x83\x5e",                       /* t ty ch ts */
   "\x83\x60",
   "\x83\x63",
   "\x83\x65",
   "\x83\x67",
   "\x83\x70",                       /* p py */
   "\x83\x73",
   "\x83\x76",
   "\x83\x79",
   "\x83\x7c",
   NULL
};

static const char *njd_set_unvoiced_vowel_candidate_list3[] = {
   "\x83\x4c\x83\x85",                    /* ky u */
   "\x83\x56\x83\x85",                    /* sh u */
   "\x83\x60\x83\x85",                    /* ch u */
   "\x83\x63\x83\x42",                    /* ts i */
   "\x83\x71\x83\x85",                    /* hy u */
   "\x83\x73\x83\x85",                    /* py u */
   "\x83\x65\x83\x85",                    /* ty u */
   "\x83\x67\x83\x44",                    /* t u */
   "\x83\x65\x83\x42",                    /* t i */
   "\x83\x4c",                       /* k i */
   "\x83\x4e",                       /* k u */
   "\x83\x56",                       /* sh i */
   "\x83\x60",                       /* ch i */
   "\x83\x63",                       /* ts u */
   "\x83\x73",                       /* p i */
   "\x83\x76",                       /* p u */
   NULL
};

static const char *njd_set_unvoiced_vowel_next_mora_list3[] = {
   "\x83\x4a",                       /* k ky */
   "\x83\x4c",
   "\x83\x4e",
   "\x83\x50",
   "\x83\x52",
   "\x83\x54",                       /* s sh */
   "\x83\x56",
   "\x83\x58",
   "\x83\x5a",
   "\x83\x5c",
   "\x83\x5e",                       /* t ty ch ts */
   "\x83\x60",
   "\x83\x63",
   "\x83\x65",
   "\x83\x67",
   "\x83\x6e",                       /* h f hy */
   "\x83\x71",
   "\x83\x74",
   "\x83\x77",
   "\x83\x7a",
   "\x83\x70",                       /* p py */
   "\x83\x73",
   "\x83\x76",
   "\x83\x79",
   "\x83\x7c",
   NULL
};

static const char *njd_set_unvoiced_vowel_mora_list[] = {
   "\x83\x94\x83\x87",
   "\x83\x94\x83\x85",
   "\x83\x94\x83\x83",
   "\x83\x94\x83\x48",
   "\x83\x94\x83\x46",
   "\x83\x94\x83\x42",
   "\x83\x94\x83\x40",
   "\x83\x94",
   "\x83\x93",
   "\x83\x92",
   "\x83\x91",
   "\x83\x90",
   "\x83\x8f",
   "\x83\x8d",
   "\x83\x8c",
   "\x83\x8b",
   "\x83\x8a\x83\x87",
   "\x83\x8a\x83\x85",
   "\x83\x8a\x83\x83",
   "\x83\x8a\x83\x46",
   "\x83\x8a",
   "\x83\x89",
   "\x83\x88",
   "\x83\x87",
   "\x83\x86",
   "\x83\x85",
   "\x83\x84",
   "\x83\x83",
   "\x83\x82",
   "\x83\x81",
   "\x83\x80",
   "\x83\x7e\x83\x87",
   "\x83\x7e\x83\x85",
   "\x83\x7e\x83\x83",
   "\x83\x7e\x83\x46",
   "\x83\x7e",
   "\x83\x7d",
   "\x83\x7c",
   "\x83\x7b",
   "\x83\x7a",
   "\x83\x79",
   "\x83\x78",
   "\x83\x77",
   "\x83\x76",
   "\x83\x75",
   "\x83\x74\x83\x48",
   "\x83\x74\x83\x46",
   "\x83\x74\x83\x42",
   "\x83\x74\x83\x40",
   "\x83\x74",
   "\x83\x73\x83\x87",
   "\x83\x73\x83\x85",
   "\x83\x73\x83\x83",
   "\x83\x73\x83\x46",
   "\x83\x73",
   "\x83\x72\x83\x87",
   "\x83\x72\x83\x85",
   "\x83\x72\x83\x83",
   "\x83\x72\x83\x46",
   "\x83\x72",
   "\x83\x71\x83\x87",
   "\x83\x71\x83\x85",
   "\x83\x71\x83\x83",
   "\x83\x71\x83\x46",
   "\x83\x71",
   "\x83\x70",
   "\x83\x6f",
   "\x83\x6e",
   "\x83\x6d",
   "\x83\x6c",
   "\x83\x6b",
   "\x83\x6a\x83\x87",
   "\x83\x6a\x83\x85",
   "\x83\x6a\x83\x83",
   "\x83\x6a\x83\x46",
   "\x83\x6a",
   "\x83\x69",
   "\x83\x68\x83\x44",
   "\x83\x68",
   "\x83\x67\x83\x44",
   "\x83\x67",
   "\x83\x66\x83\x87",
   "\x83\x66\x83\x85",
   "\x83\x66\x83\x83",
   "\x83\x66\x83\x42",
   "\x83\x66",
   "\x83\x65\x83\x87",
   "\x83\x65\x83\x85",
   "\x83\x65\x83\x83",
   "\x83\x65\x83\x42",
   "\x83\x65",
   "\x83\x64",
   "\x83\x63\x83\x48",
   "\x83\x63\x83\x46",
   "\x83\x63\x83\x42",
   "\x83\x63\x83\x40",
   "\x83\x63",
   "\x83\x62",
   "\x83\x61",
   "\x83\x60\x83\x87",
   "\x83\x60\x83\x85",
   "\x83\x60\x83\x83",
   "\x83\x60\x83\x46",
   "\x83\x60",
   "\x83\x5f",
   "\x83\x5e",
   "\x83\x5d",
   "\x83\x5c",
   "\x83\x5b",
   "\x83\x5a",
   "\x83\x59\x83\x42",
   "\x83\x59",
   "\x83\x58\x83\x42",
   "\x83\x58",
   "\x83\x57\x83\x87",
   "\x83\x57\x83\x85",
   "\x83\x57\x83\x83",
   "\x83\x57\x83\x46",
   "\x83\x57",
   "\x83\x56\x83\x87",
   "\x83\x56\x83\x85",
   "\x83\x56\x83\x83",
   "\x83\x56\x83\x46",
   "\x83\x56",
   "\x83\x55",
   "\x83\x54",
   "\x83\x53",
   "\x83\x52",
   "\x83\x51",
   "\x83\x50",
   "\x83\x4f",
   "\x83\x4e",
   "\x83\x4d\x83\x87",
   "\x83\x4d\x83\x85",
   "\x83\x4d\x83\x83",
   "\x83\x4d\x83\x46",
   "\x83\x4d",
   "\x83\x4c\x83\x87",
   "\x83\x4c\x83\x85",
   "\x83\x4c\x83\x83",
   "\x83\x4c\x83\x46",
   "\x83\x4c",
   "\x83\x4b",
   "\x83\x4a",
   "\x83\x49",
   "\x83\x48",
   "\x83\x47",
   "\x83\x46",
   "\x83\x45\x83\x48",
   "\x83\x45\x83\x46",
   "\x83\x45\x83\x42",
   "\x83\x45",
   "\x83\x44",
   "\x83\x43\x83\x46",
   "\x83\x43",
   "\x83\x42",
   "\x83\x41",
   "\x83\x40",
   "\x81\x5b",
   NULL
};

NJD_SET_UNVOICED_VOWEL_RULE_H_END;

#endif                          /* !NJD_SET_UNVOICED_VOWEL_RULE_H */
