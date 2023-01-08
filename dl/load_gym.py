from pythonnet import load
load("coreclr")

import clr

def load_gym():
    clr.AddReference('./gym/G5Gym')
    from G5Gym import GymExports
    return GymExports()

def test():
    gym = load_gym()
    gym.testCall()

    #arr = gym.testCallArray()
    #print(arr)
    #print(len(arr))
    #print(arr[0])
    #print(arr[1])

    obj = gym.testCallStruct()
    print(f"testCallStruct {obj}")

    state = gym.startHand()

    print(f"State {state}")

    print(f"Pot Size: {state.pot_size}")
    print(f"Ammount to call: {state.ammount_to_call}")

    print("HoleCards:")
    print(state.hole_cards)

    print("Board:")
    for i in range(state.board.Count):
        print(state.board[i])

    print("Actions:")
    for i in range(state.actions.Count):
        print(state.actions[i])

test()