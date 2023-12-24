from pythonnet import load
load("coreclr")
import clr
clr.AddReference("G5Gym")

# from G5Gym import GymExports
# instance = GymExports()

from G5Gym import PythonAPI

g5pythonApi = PythonAPI(numPlayers=6, bigBlindSize=4)

print(g5pythonApi.testCallStruct())


g5pythonApi.createGame(
    playerNames=["Pl1", "Pl2", "Pl3", "Pl4", "Pl5", "Pl6"],
    stackSizes=[100, 100, 100, 100, 100, 100],
    heroInd=0, 
    buttonInd=1, 
    bigBlindSize=4)

