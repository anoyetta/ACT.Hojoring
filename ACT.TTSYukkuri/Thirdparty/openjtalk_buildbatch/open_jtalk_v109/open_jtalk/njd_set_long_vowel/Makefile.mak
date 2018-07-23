TARGET = libnjd_set_long_vowel.lib
OBJS = njd_set_long_vowel.obj
INC = /I ..\njd
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
