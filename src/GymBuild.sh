#!/bin/bash

# Build DecisionMaking dll
cd DecisionMaking
make
cd ..

# Clean contents of bin dir
mkdir bin
cd bin
rm -rf *
cd ..

# Build the dotnet projects...
cd G5Gym
dotnet restore
dotnet publish -c Release -o ../../dl/gym -f net6.0 -r ubuntu.20.04-x64
cd ..

# Copy all redist files (probabilities etc)
cp DecisionMaking/libdec_making.so ../dl/gym/DecisionMaking.dll
cp ../redist/PreFlopEquities.txt ../dl/gym/
cp ../redist/full_stats_list_6max.bin ../dl/gym/
cp ../redist/full_stats_list_6max_2m.bin ../dl/gym/
cp ../redist/full_stats_list_hu.bin ../dl/gym/
