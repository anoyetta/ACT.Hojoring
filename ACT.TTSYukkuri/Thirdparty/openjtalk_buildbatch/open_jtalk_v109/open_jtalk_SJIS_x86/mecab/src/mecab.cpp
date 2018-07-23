//  MeCab -- Yet Another Part-of-Speech and Morphological Analyzer
//
//
//  Copyright(C) 2001-2006 Taku Kudo <taku@chasen.org>
//  Copyright(C) 2004-2006 Nippon Telegraph and Telephone Corporation

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

#ifndef MECAB_CPP
#define MECAB_CPP

#include <stdlib.h>
#include <string.h>

#include "mecab.h"

#ifdef __cplusplus
#define MECAB_CPP_START extern "C" {
#define MECAB_CPP_END   }
#else
#define MECAB_CPP_START
#define MECAB_CPP_END
#endif                          /* __CPLUSPLUS */

MECAB_CPP_START;

BOOL Mecab_initialize(Mecab *m){
  m->feature = NULL;
  m->size = 0;
  m->mecab = NULL;
  return TRUE;
}

BOOL Mecab_load(Mecab *m, const char *dicdir){
  int i;
  int argc = 3;
  char **argv;

  if(m == NULL)
    return FALSE;

  if(m->mecab != NULL)
    Mecab_clear(m);

  if(dicdir == NULL || strlen(dicdir) == 0)
    return FALSE;

  argv = (char **) malloc(sizeof(char *) * argc);

  argv[0] = strdup("mecab");
  argv[1] = strdup("-d");
  argv[2] = strdup(dicdir);

  m->mecab = mecab_new(argc, argv);

  for(i = 0;i < argc;i++)
    free(argv[i]);
  free(argv);

  if(m->mecab == NULL){
    fprintf(stderr,"ERROR: Mecab_load() in mecab.cpp: Cannot open %s.\n",dicdir);
    return FALSE;
  }
  return TRUE;
}

BOOL Mecab_analysis(Mecab *m, const char *str){
  int i = 0;
  mecab_node_t *head;
  mecab_node_t *node;

  if(m->mecab == NULL)
    return FALSE;

  if(m->size > 0 || m->feature != NULL)
    Mecab_refresh(m);

  head = (mecab_node_t *) mecab_sparse_tonode(m->mecab, str);
  if(head == NULL) return FALSE;
  for (node = head; node != NULL; node = node->next) {
    if(node->stat != MECAB_BOS_NODE && node->stat != MECAB_EOS_NODE)
      m->size++;
  }
  m->feature = (char **) calloc(m->size, sizeof(char *));
  for (node = head; node != NULL; node = node->next) {
    if(node->stat != MECAB_BOS_NODE && node->stat != MECAB_EOS_NODE){
      m->feature[i] = (char *) calloc(node->length + strlen(node->feature) + 2,sizeof(char));
      strcpy(m->feature[i],"");
      strncat(m->feature[i],node->surface,node->length);
      strcat(m->feature[i],",");
      strcat(m->feature[i],node->feature);
      i++;
    }
  }
  return TRUE;
}

BOOL Mecab_print(Mecab *m){
  int i;
  
  for(i = 0;i < m->size;i++)
    printf("%s\n",m->feature[i]);
  return TRUE;
}

int Mecab_get_size(Mecab *m){
  return m->size;
}

char **Mecab_get_feature(Mecab *m){
  return m->feature;
}

BOOL Mecab_refresh(Mecab *m){
  int i;
  
  if(m->feature != NULL){
    for(i = 0;i < m->size;i++)
      free(m->feature[i]);
    free(m->feature);
    m->feature = NULL;
    m->size = 0;
  }

  return TRUE;
}

BOOL Mecab_clear(Mecab *m){
  Mecab_refresh(m);
  if(m->mecab != NULL){
    mecab_destroy(m->mecab);
    m->mecab = NULL;
  }
  return TRUE;
}

MECAB_CPP_END;

#endif                          /* !MECAB_CPP */
