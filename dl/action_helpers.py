from random import randint


NUM_RANKS = 13
NUM_SUITES = 4

ACTION_FOLD = ["f", -1]
ACTION_CC = ["cc", 0]
ACTION_BR = ["br", 1]
ACTION_ALLIN = ["ai", 2]


def calc_raise_ammount(pot_size, ammount_to_call, stack_size):
    res = pot_size + 2 * ammount_to_call
    return min(res, stack_size)


def randomize_action(pot_size, ammount_to_call, stack_size, never_fold=False):

    rnd = randint(0, 99)
    print("Rnd action: ", rnd)

    # 5 percent all in
    if rnd < 5:
        return ACTION_ALLIN, stack_size

    raise_ammount = calc_raise_ammount(pot_size, ammount_to_call, stack_size)

    if ammount_to_call == 0 or never_fold:
        if rnd < 50:
            return ACTION_CC, ammount_to_call
        else:
            return ACTION_BR, raise_ammount
    else:
        if rnd < 33:
            return ACTION_FOLD, 0
        elif rnd < 67:
            return ACTION_CC, ammount_to_call
        else:
            return ACTION_BR, raise_ammount


def calculate_action(predictions, pot_size, ammount_to_call, stack_size):

    # Check call
    ccEv = predictions[0][0] - ammount_to_call
    print("Check call prediction:", round(predictions[0][0], 2), " ammount to call:", ammount_to_call)

    # Bet-raise
    raise_ammount = calc_raise_ammount(pot_size, ammount_to_call, stack_size)
    brEv = predictions[1][0] - raise_ammount
    print("Bet-raise pred:", round(predictions[1][0], 2), " raise ammount:", raise_ammount)

    # All-in
    aiEv = predictions[2][0] - stack_size
    print("All in prediction:", round(predictions[2][0], 2), " stack size:", stack_size)

    maxEv = max(ccEv, brEv, aiEv)
    print("Max ev:", round(maxEv, 2))

    if maxEv < 0:
        return ACTION_FOLD, 0
    elif maxEv == ccEv:
        return ACTION_CC, ammount_to_call
    elif maxEv == brEv:
        return ACTION_BR, raise_ammount
    elif maxEv == aiEv:
        return ACTION_ALLIN, stack_size

    print("Invalid Path !!!!!!!! ------------ ")
    return ACTION_FOLD, 0