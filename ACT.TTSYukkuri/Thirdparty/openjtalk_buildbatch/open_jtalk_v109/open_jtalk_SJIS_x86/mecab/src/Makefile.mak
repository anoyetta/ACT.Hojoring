TARGET = libmecab.lib mecab-dict-index.exe
OBJS = char_property.obj \
connector.obj \
context_id.obj \
dictionary.obj \
dictionary_compiler.obj \
dictionary_generator.obj \
dictionary_rewriter.obj \
eval.obj \
feature_index.obj \
iconv_utils.obj \
lbfgs.obj \
learner.obj \
learner_tagger.obj \
libmecab.obj \
nbest_generator.obj \
param.obj \
string_buffer.obj \
tagger.obj \
tokenizer.obj \
utils.obj \
viterbi.obj \
writer.obj
LIBS = libmecab.lib Advapi32.lib

!include ..\..\macro.inc

.SUFFIXES: .c .cpp .obj

all : $(TARGET)

libmecab.lib : $(OBJS) mecab.obj
	$(CL) /lib $(LFLAGS) /OUT:$@ $(OBJS)

libmecab.dll : $(OBJS) mecab.obj
	$(CL) /dll $(LFLAGS) /IMPLIB:$(@B).lib /OUT:$@ $(OBJS) advapi32.lib
	copy $@ ..\..\..\

mecab-dict-index.exe : mecab-dict-index.obj libmecab.lib
	$(CL) $(LFLAGS) /OUT:$@ $(LIBS) $(@B).obj

.cpp.obj:
	$(CC) $(MECAB_CFLAGS) /c $<

clean:
	del *.dll
	del *.lib
	del *.obj


