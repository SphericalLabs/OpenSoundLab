SRCS=AudioPluginUtil.cpp \
../util.c \
../RingBuffer.cpp \
MasterBusRecorder.cpp
OBJS=$(SRCS:.cpp=.o)

OUTPUT=libMasterBusRecorder.so
CXXFLAGS=-I. -I.. -O2 -fPIC
LDFLAGS=-shared -rdynamic -fPIC
CXX=g++

all: $(OUTPUT)
clean:
	rm -f $(OUTPUT) $(OBJS)

$(OUTPUT): $(OBJS)
	$(CXX) $(LDFLAGS) -o $(OUTPUT) $(OBJS)

.cpp.o:
	$(CXX) $(CXXFLAGS) -c $<
