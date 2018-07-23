# Process this file with autoconf to produce a configure script.
AC_PREREQ(2.59)
AC_INIT(open_jtalk, 1.09, open-jtalk-users@lists.sourceforge.net)
AC_CONFIG_AUX_DIR([config])
AC_COPYRIGHT(Copyright 2008-2015 Nagoya Institute of Technology)
AM_INIT_AUTOMAKE

# Checks for C/C++ compiler
AC_PROG_CC
AC_PROG_CXX
AM_PROG_CC_C_O
AC_PROG_INSTALL
AC_PROG_RANLIB
AN_MAKEVAR([AR], [AC_PROG_AR])
AN_PROGRAM([ar], [AC_PROG_AR])
AC_DEFUN([AC_PROG_AR], [AC_CHECK_TOOL(AR, ar, :)])
AC_PROG_AR

# Check for endian
AC_C_BIGENDIAN

# Checks for libraries.
AC_CHECK_LIB([m], [log])
AC_CHECK_LIB(stdc++, main, MECAB_LIBS="-lstdc++")
AC_CHECK_LIB(iconv,iconv_open,use_libiconv=yes)
AC_CHECK_FUNC(iconv_open,use_iconv=yes)

# Checks for header files.
AC_HEADER_STDC
AC_CHECK_HEADERS(stdlib.h string.h unistd.h fcntl.h sys/stat.h sys/mman.h sys/times.h sys/types.h dirent.h ctype.h sys/types.h io.h windows.h setjmp.h)

# Checks for typedefs, structures, and compiler characteristics.
AC_C_CONST
AC_TYPE_SIZE_T

# Checks for library functions.
AC_FUNC_MMAP
AC_CHECK_FUNCS(sqrt)
AC_CHECK_FUNCS(strstr)
AC_CHECK_FUNCS(getenv)
AC_CHECK_FUNCS(opendir)
AC_CHECK_FUNCS(setjmp)

# Set compilier option for mecab
MECAB_DEFAULT_RC="dummy"
AC_SUBST(MECAB_DEFAULT_RC)
AC_CONFIG_HEADERS([mecab/config.h:mecab/config.h.in])
DIC_VERSION="102"
AC_SUBST(DIC_VERSION)
AC_PROG_GCC_TRADITIONAL
AC_PROG_MAKE_SET
AC_ISC_POSIX
AC_CYGWIN
AC_LANG_CPLUSPLUS
AC_LANG(C)
AC_TYPE_OFF_T
if test "$use_libiconv" == "yes"; then
   MECAB_LIBS="$MECAB_LIBS -liconv"
   AC_DEFINE(HAVE_ICONV,1,Define to 1 if you have the `iconv_open' library(-liconv).)
   AC_DEFINE(ICONV_CONST,,Define to 1 if you have the `iconv_open' library(-liconv).)
fi
if test "$use_iconv" == "yes"; then
   AC_DEFINE(HAVE_ICONV,1,Define to 1 if you have the `iconv_open' library(-liconv).)
   AC_DEFINE(ICONV_CONST,,Define to 1 if you have the `iconv_open' library(-liconv).)
fi
AC_TYPE_SIZE_T
AC_CHECK_SIZEOF(char)
AC_CHECK_SIZEOF(short)
AC_CHECK_SIZEOF(int)
AC_CHECK_SIZEOF(long)
AC_CHECK_SIZEOF(long long)
AC_CHECK_SIZEOF(size_t)
AC_TYPE_SIZE_T
AC_LANG(C++)
LIBS="$LIBS $MECAB_LIBS"
AC_SUBST(MECAB_LIBS)
AC_MSG_CHECKING(whether make is GNU Make)
if $ac_make --version 2>/dev/null | grep '^GNU Make ' >/dev/null ; then
   AC_MSG_RESULT(yes)
else
   AC_MSG_RESULT(no)
   if test "$host_vendor" = "sun" ; then
      AC_MSG_ERROR("SUN make does not work for building MeCab. Please install GNU make")
   fi
fi
AC_MSG_CHECKING([if ${CXX-c++} supports template <class T> (required)])
AC_TRY_COMPILE(
[
   template <class T> T foo (T &i) { return i++; };
],[
   int i = 0;
   double d = 0.0;
   foo(i); foo(d);
],[
   ac_template=yes
],[
   AC_WARN(${CXX-c++} template <class T> does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_template])
AC_MSG_CHECKING([if ${CXX-c++} supports const_cast<> (required)])
AC_TRY_COMPILE(
[
   class foo;
],[
   const foo *c=0;
   foo *c1=const_cast<foo*>(c);
],[
   ac_const_cast=yes
],[
   AC_WARN(${CXX-c++} const_cast<> does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_const_cast])
AC_MSG_CHECKING(if ${CXX-c++} supports static_cast<> (required))
AC_TRY_COMPILE(
[
   class foo;
],[
   foo *c = 0;
   void *c1 = static_cast<void *>(c);
],[
   ac_static_cast=yes
],[
   AC_WARN(${CXX-c++} static_cast<> does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_static_cast])
AC_MSG_CHECKING(if ${CXX-c++} supports dynamic_cast<> (required))
AC_TRY_COMPILE(
[
   class foo {};
   class bar: public foo {};
],[
   bar *c = 0;
   foo *c1 = dynamic_cast<foo *>(c);
],[
   ac_dynamic_cast=yes
],[
   AC_WARN(${CXX-c++} dynamic_cast<> does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_dynamic_cast])
AC_MSG_CHECKING(if ${CXX-c++} supports reinterpret_cast<> (required))
AC_TRY_COMPILE(
[
   int *a = 0;
],[
   char *p = reinterpret_cast<char *>(a);
],[
   ac_reinterpret_cast=yes
],[
   AC_WARN(${CXX-c++} reinterpret_cast<> does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_reinterpret_cast])
AC_MSG_CHECKING(if ${CXX-c++} supports exception handler (required))
AC_TRY_COMPILE(
[
   ;
],[
   try {
      int i = 0;
   }
   catch (char *e) {
   }
   catch (...) {
   }
],[
   ac_exception=yes
],[
   AC_WARN(${CXX-c++} exception does not work)
   config_error=yes
])
AC_MSG_RESULT([$ac_exception])
AC_MSG_CHECKING(if ${CXX-c++} supports namespaces (required) )
AC_TRY_COMPILE(
[
   namespace Foo { struct A {}; }
   using namespace Foo;
],[
   A a;
],[
   ac_namespaces=yes
],[
   config_error=yes
   ac_namespaces=no
])
AC_MSG_RESULT([$ac_namespaces])
AC_MSG_CHECKING(if ${CXX-c++} environment provides all required features)
if test "x$config_error" = xyes ; then
   AC_MSG_RESULT([no])
   AC_MSG_ERROR([Your compiler is not powerful enough to compile MeCab. \
                 If it should be, see config.log for more information of why it failed.])
fi
AC_MSG_RESULT([yes])

# For cygwin
if test -n "$GCC"; then
case "$host_os" in
   *cygwin* )
      CFLAGS="-O2 -Wall -g -D__CYGWIN__ $CFLAGS "
      CXXFLAGS="-O3 -Wall -Wno-deprecated -D__CYGWIN__ $CXXFLAGS "
      ;;
   *)
      CFLAGS="-O2 -Wall -g $CFLAGS"
      CXXFLAGS="-O3 -Wall -Wno-deprecated $CXXFLAGS "
      ;;
esac
fi

# Define HTS_ENGINE_HEADER
AC_ARG_WITH(hts-engine-header-path,
            AS_HELP_STRING([--with-hts-engine-header-path],[specify the hts_engine_API header path]),
            [hts_engine_header_path=$withval],
            [hts_engine_header_path='/usr/local/include'])
if test ! -f "${hts_engine_header_path}/HTS_engine.h"; then
   AC_MSG_ERROR(Cannot find HTS_engine.h)
fi
AC_MSG_CHECKING(for HTS_engine.h)
AC_MSG_RESULT($hts_engine_header_path/HTS_engine.h)
AC_SUBST(HTS_ENGINE_HEADER)
HTS_ENGINE_HEADER=${hts_engine_header_path}/HTS_engine.h

# Define HTS_ENGINE_HEADER_DIR
AC_SUBST(HTS_ENGINE_HEADER_DIR)
HTS_ENGINE_HEADER_DIR=${hts_engine_header_path}

# Define HTS_ENGINE_LIBRARY
AC_ARG_WITH(hts-engine-library-path,
            AS_HELP_STRING([--with-hts-engine-library-path],[specify the hts_engine_API library path]),
            [hts_engine_library_path=$withval],
            [hts_engine_library_path='/usr/local/lib'])
if test ! -f "${hts_engine_library_path}/libHTSEngine.a"; then
   AC_MSG_ERROR(Cannot find libHTSEngine.a)
fi
AC_MSG_CHECKING(for libHTSEngine.a)
AC_MSG_RESULT($hts_engine_library_path/libHTSEngine.a)
AC_SUBST(HTS_ENGINE_LIBRARY)
HTS_ENGINE_LIBRARY=${hts_engine_library_path}/libHTSEngine.a

# Define HTS_ENGINE_LIBRARY_DIR
AC_SUBST(HTS_ENGINE_LIBRARY_DIR)
HTS_ENGINE_LIBRARY_DIR=${hts_engine_library_path}

# Check for windows audio devices
case "$host_os" in
   *win32* | *wince* | *cygwin* | *mingw* )
      AC_HAVE_LIBRARY([winmm],,AC_MSG_ERROR(No winmm))
      ;;
   *)
      ;;
esac

# Define charset for mecab-naist-dic
AC_ARG_WITH(charset,
            [  --with-charset          specify charset (EUC-JP/SHIFT_JIS/UTF-8)],
            [charset=$withval],
            [charset='UTF-8'])
AC_MSG_CHECKING(charset)
AC_MSG_RESULT($charset)
AC_SUBST(MECAB_CHARSET)
AC_SUBST(MECAB_USE_UTF8_ONLY)
if test x$charset = xeucjp -o x$charset = xeuc-jp -o x$charset = xeuc_jp -o x$charset = xEUCJP -o x$charset = xEUC-JP -o x$charset = xEUC_JP ; then
   MECAB_CHARSET=euc-jp
   MECAB_USE_UTF8_ONLY=""
elif test x$charset = xsjis -o x$charset = xshift-jis -o x$charset = xshift_jis -o x$charset = xSJIS -o x$charset = xSHIFT-JIS -o x$charset = xSHIFT_JIS ; then
   MECAB_CHARSET=sjis
   MECAB_USE_UTF8_ONLY=""
elif test x$charset = xutf8 -o x$charset = xutf-8 -o x$charset = xutf_8 -o x$charset = xUTF8 -o x$charset = xUTF-8 -o x$charset = xUTF_8 ; then
   MECAB_CHARSET=utf-8
   MECAB_USE_UTF8_ONLY="-D MECAB_USE_UTF8_ONLY"
else
   AC_MSG_ERROR(Cannot recognize $charset)
fi

# Define charset for Open JTalk
AC_SUBST(CHARSET)
if test x$charset = xeucjp -o x$charset = xeuc-jp -o x$charset = xeuc_jp -o x$charset = xEUCJP -o x$charset = xEUC-JP -o x$charset = xEUC_JP ; then
   CHARSET="-D CHARSET_EUC_JP"
   CFLAGS="$CFLAGS -finput-charset=EUC-JP -fexec-charset=EUC-JP "
   CXXFLAGS="$CXXFLAGS -finput-charset=EUC-JP -fexec-charset=EUC-JP "
elif test x$charset = xsjis -o x$charset = xshift-jis -o x$charset = xshift_jis -o x$charset = xSJIS -o x$charset = xSHIFT-JIS -o x$charset = xSHIFT_JIS ; then
   CHARSET="-D CHARSET_SHIFT_JIS"
   CFLAGS="$CFLAGS -finput-charset=cp932 -fexec-charset=cp932 "
   CXXFLAGS="$CXXFLAGS -finput-charset=cp932 -fexec-charset=cp932 "
elif test x$charset = xutf8 -o x$charset = xutf-8 -o x$charset = xutf_8 -o x$charset = xUTF8 -o x$charset = xUTF-8 -o x$charset = xUTF_8 ; then
   CHARSET="-D CHARSET_UTF_8"
   CFLAGS="$CFLAGS -finput-charset=UTF-8 -fexec-charset=UTF-8 "
   CXXFLAGS="$CXXFLAGS -finput-charset=UTF-8 -fexec-charset=UTF-8 "
else
   AC_MSG_ERROR(Cannot recognize $charset)
fi

# Makefiles
AC_CONFIG_FILES([Makefile bin/Makefile text2mecab/Makefile mecab/Makefile mecab/src/Makefile mecab2njd/Makefile njd/Makefile njd_set_pronunciation/Makefile njd_set_digit/Makefile njd_set_accent_phrase/Makefile njd_set_accent_type/Makefile njd_set_unvoiced_vowel/Makefile njd_set_long_vowel/Makefile njd2jpcommon/Makefile jpcommon/Makefile mecab-naist-jdic/Makefile])

# Make mecab/config.h
AC_OUTPUT
