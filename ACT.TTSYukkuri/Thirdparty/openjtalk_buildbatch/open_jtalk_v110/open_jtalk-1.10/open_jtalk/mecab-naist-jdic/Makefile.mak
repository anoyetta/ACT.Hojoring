TARGET = char.bin matrix.bin sys.dic unk.dic
CSV = naist-jdic.csv
DEFS = matrix.def left-id.def pos-id.def rewrite.def right-id.def char.def unk.def feature.def
CONVERTER=..\mecab\src\mecab-dict-index.exe
!include ..\macro.inc

all : $(TARGET)

$(TARGET) : $(OBJS) $(CSV) $(DEFS)
	$(CONVERTER) -d . -o . -f EUC-JP -t $(CHARSET)

clean:
	del $(TARGET)
