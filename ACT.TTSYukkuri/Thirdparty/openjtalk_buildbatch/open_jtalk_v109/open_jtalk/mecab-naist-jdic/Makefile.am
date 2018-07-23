
EXTRA_DIST = COPYING Makefile.mak naist-jdic.csv matrix.def left-id.def pos-id.def rewrite.def right-id.def char.def unk.def feature.def

DISTCLEANFILES = *.log *.out *~

MAINTAINERCLEANFILES = Makefile.in char.bin matrix.bin sys.dic unk.dic

dicdir = @prefix@/dic

dic_DATA   = char.bin matrix.bin sys.dic unk.dic

char.bin matrix.bin sys.dic unk.dic: naist-jdic.csv matrix.def left-id.def rewrite.def pos-id.def right-id.def char.def unk.def feature.def
	../mecab/src/mecab-dict-index -d . -o . -f UTF-8 -t @MECAB_CHARSET@

clean:
	rm -f char.bin matrix.bin sys.dic unk.dic
