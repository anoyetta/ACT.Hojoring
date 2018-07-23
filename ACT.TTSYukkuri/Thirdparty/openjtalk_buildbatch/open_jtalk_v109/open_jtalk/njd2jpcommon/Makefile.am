
EXTRA_DIST = Makefile.mak

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in

AM_CPPFLAGS = -I @top_srcdir@/njd -I @top_srcdir@/jpcommon @CHARSET@

noinst_LIBRARIES = libnjd2jpcommon.a

libnjd2jpcommon_a_SOURCES = njd2jpcommon.h \
                            njd2jpcommon.c \
                            njd2jpcommon_rule_ascii_for_euc_jp.h \
                            njd2jpcommon_rule_ascii_for_shift_jis.h \
                            njd2jpcommon_rule_ascii_for_utf_8.h \
                            njd2jpcommon_rule_euc_jp.h \
                            njd2jpcommon_rule_shift_jis.h \
                            njd2jpcommon_rule_utf_8.h
