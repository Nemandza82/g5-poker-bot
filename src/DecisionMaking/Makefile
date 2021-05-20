OPT_FLAGS = -std=c++11 -O2 -s -DNDEBUG

output: DecisionMaking.o ForExport.o PreFlopEquity.o Range.o GameState.o GameContext.o Pot.o PlayerModel.o PostFlopParams.o PreFlopParams.o AllHandStrengths.o AllCounters.o HandStrengthCounter.o HandStrength.o Card.o Common.o
	g++ -shared $(OPT_FLAGS) DecisionMaking.o ForExport.o PreFlopEquity.o Range.o GameState.o GameContext.o Pot.o PlayerModel.o PostFlopParams.o PreFlopParams.o AllHandStrengths.o AllCounters.o HandStrengthCounter.o HandStrength.o Card.o Common.o -o libdec_making.so -ltbb

COMPILE_FLAGS = $(OPT_FLAGS) -c -fPIC

AllCounters.o: AllCounters.cpp AllCounters.h
	g++ $(COMPILE_FLAGS) AllCounters.cpp

AllHandStrengths.o: AllHandStrengths.cpp AllHandStrengths.h
	g++ $(COMPILE_FLAGS) AllHandStrengths.cpp

Card.o: Card.cpp Card.h
	g++ $(COMPILE_FLAGS) Card.cpp

Common.o: Common.cpp Common.h
	g++ $(COMPILE_FLAGS) Common.cpp

DecisionMaking.o: DecisionMaking.cpp
	g++ $(COMPILE_FLAGS) DecisionMaking.cpp

ForExport.o: ForExport.cpp
	g++ $(COMPILE_FLAGS) ForExport.cpp

GameContext.o: GameContext.cpp GameContext.h
	g++ $(COMPILE_FLAGS) GameContext.cpp

GameState.o: GameState.cpp GameState.h
	g++ $(COMPILE_FLAGS) GameState.cpp

HandStrength.o: HandStrength.cpp HandStrength.h
	g++ $(COMPILE_FLAGS) HandStrength.cpp

HandStrengthCounter.o: HandStrengthCounter.cpp HandStrengthCounter.h
	g++ $(COMPILE_FLAGS) HandStrengthCounter.cpp

PlayerModel.o: PlayerModel.cpp PlayerModel.h
	g++ $(COMPILE_FLAGS) PlayerModel.cpp

PostFlopParams.o: PostFlopParams.cpp PostFlopParams.h
	g++ $(COMPILE_FLAGS) PostFlopParams.cpp

Pot.o: Pot.cpp Pot.h
	g++ $(COMPILE_FLAGS) Pot.cpp

PreFlopEquity.o: PreFlopEquity.cpp PreFlopEquity.h
	g++ $(COMPILE_FLAGS) PreFlopEquity.cpp

PreFlopParams.o: PreFlopParams.cpp PreFlopParams.h
	g++ $(COMPILE_FLAGS) PreFlopParams.cpp

Range.o: Range.cpp Range.h
	g++ $(COMPILE_FLAGS) Range.cpp

clean:
	rm *.o
