TARGET = hts_engine_API.lib
OBJS = \
HTS_audio.obj \
HTS_engine.obj \
HTS_gstream.obj \
HTS_label.obj \
HTS_misc.obj \
HTS_model.obj \
HTS_pstream.obj \
HTS_sstream.obj \
HTS_vocoder.obj
INC  = /I ..\include
!include ..\macro.inc


.SUFFIXES: .c .obj

all : $(TARGET)

$(TARGET) : $(OBJS)
	$(CL) /lib $(LFLAGS) /OUT:$@ $(OBJS)

.c.obj:
	$(CC) $(CFLAGS) /c $<

clean:
	del *.lib
	del *.obj
