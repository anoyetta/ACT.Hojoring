TARGET = libnjd_set_digit.lib
OBJS = njd_set_digit.obj
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
