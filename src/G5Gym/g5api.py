from pythonnet import load
load("coreclr")
import clr
clr.AddReference("G5Gym")

# from G5Gym import GymExports
# instance = GymExports()

from G5Gym import PythonAPI

g5pythonApi = PythonAPI(6, 4)
