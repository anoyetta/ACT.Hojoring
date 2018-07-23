
EXTRA_DIST = Makefile.mak

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in

AM_CPPFLAGS = -I @top_srcdir@/njd @CHARSET@

noinst_LIBRARIES = libnjd_set_long_vowel.a

libnjd_set_long_vowel_a_SOURCES = njd_set_long_vowel.h \
                                  njd_set_long_vowel.c \
                                  njd_set_long_vowel_rule_ascii_for_euc_jp.h \
                                  njd_set_long_vowel_rule_ascii_for_shift_jis.h \
                                  njd_set_long_vowel_rule_ascii_for_utf_8.h \
                                  njd_set_long_vowel_rule_euc_jp.h \
                                  njd_set_long_vowel_rule_shift_jis.h \
                                  njd_set_long_vowel_rule_utf_8.h
