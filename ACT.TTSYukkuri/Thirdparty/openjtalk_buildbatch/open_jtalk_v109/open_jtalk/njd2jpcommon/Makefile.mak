TARGET = libnjd2jpcommon.lib
OBJS = njd2jpcommon.obj
INC = /I ..\njd  /I ..\jpcommon
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
