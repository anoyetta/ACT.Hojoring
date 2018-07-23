
EXTRA_DIST = Makefile.mak

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in

AM_CPPFLAGS = -I @top_srcdir@/njd @CHARSET@

noinst_LIBRARIES = libnjd_set_digit.a

libnjd_set_digit_a_SOURCES = njd_set_digit.h \
                             njd_set_digit.c \
                             njd_set_digit_rule_ascii_for_euc_jp.h \
                             njd_set_digit_rule_ascii_for_shift_jis.h \
                             njd_set_digit_rule_ascii_for_utf_8.h \
                             njd_set_digit_rule_euc_jp.h \
                             njd_set_digit_rule_shift_jis.h \
                             njd_set_digit_rule_utf_8.h
