
EXTRA_DIST = Makefile.mak

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in mecab-dict-index

AM_CPPFLAGS = -D DIC_VERSION=@DIC_VERSION@ \
              @MECAB_USE_UTF8_ONLY@ \
              -D MECAB_DEFAULT_RC="\"@MECAB_DEFAULT_RC@\"" \
              -D MECAB_DEFAULT_CHARSET="\"@MECAB_DEFAULT_CHARSET@\"" \
              -D PACKAGE="\"@PACKAGE@\"" \
              -D VERSION="\"@VERSION@\""

noinst_PROGRAMS = mecab-dict-index

mecab_dict_index_SOURCES = mecab-dict-index.cpp
mecab_dict_index_LDADD = libmecab.a

noinst_LIBRARIES = libmecab.a

libmecab_a_SOURCES = char_property.cpp \
                     char_property.h \
                     common.h \
                     connector.cpp \
                     connector.h \
                     context_id.cpp \
                     context_id.h \
                     darts.h \
                     dictionary.cpp \
                     dictionary.h \
                     dictionary_compiler.cpp \
                     dictionary_generator.cpp \
                     dictionary_rewriter.cpp \
                     dictionary_rewriter.h \
                     eval.cpp \
                     feature_index.cpp \
                     feature_index.h \
                     freelist.h \
                     iconv_utils.cpp \
                     iconv_utils.h \
                     lbfgs.cpp \
                     lbfgs.h \
                     learner.cpp \
                     learner_node.h \
                     learner_tagger.cpp \
                     learner_tagger.h \
                     libmecab.cpp \
                     mecab.cpp \
                     mecab.h \
                     mmap.h \
                     nbest_generator.cpp \
                     nbest_generator.h \
                     param.cpp \
                     param.h \
                     scoped_ptr.h \
                     stream_wrapper.h \
                     string_buffer.cpp \
                     string_buffer.h \
                     tagger.cpp \
                     thread.h \
                     tokenizer.cpp \
                     tokenizer.h \
                     ucs.h \
                     ucstable.h \
                     utils.cpp \
                     utils.h \
                     viterbi.cpp \
                     viterbi.h \
                     winmain.h \
                     writer.cpp \
                     writer.h
