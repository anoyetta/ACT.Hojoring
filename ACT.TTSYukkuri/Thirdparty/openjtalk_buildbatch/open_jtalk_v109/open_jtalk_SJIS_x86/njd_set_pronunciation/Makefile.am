
EXTRA_DIST = Makefile.mak

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in

AM_CPPFLAGS = -I @top_srcdir@/njd @CHARSET@

noinst_LIBRARIES = libnjd_set_pronunciation.a

libnjd_set_pronunciation_a_SOURCES = njd_set_pronunciation.h \
                                     njd_set_pronunciation.c \
                                     njd_set_pronunciation_rule_ascii_for_euc_jp.h \
                                     njd_set_pronunciation_rule_ascii_for_shift_jis.h \
                                     njd_set_pronunciation_rule_ascii_for_utf_8.h \
                                     njd_set_pronunciation_rule_euc_jp.h \
                                     njd_set_pronunciation_rule_shift_jis.h \
                                     njd_set_pronunciation_rule_utf_8.h
