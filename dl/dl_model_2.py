import tensorflow as tf
from tensorflow import keras
import numpy as np
import action_helpers as ah



def card_inputs(id):
    input_rank = keras.layers.Input(shape=(ah.NUM_RANKS,), dtype='float32', name=id + "_rank")
    input_suite = keras.layers.Input(shape=(ah.NUM_SUITES,), dtype='float32', name=id + "_suite")

    return input_rank, input_suite, net


def holecards_subnet(shared_dense, num_activations):
    c0r, c0s, hc0 = card_subnet(shared_dense, "hc0")
    c1r, c1s, hc1 = card_subnet(shared_dense, "hc1")
    
    hole_cards = keras.layers.concatenate([hc0, hc1])
    net = keras.layers.Dense(num_activations, activation='relu')(hole_cards)

    return c0r, c0s, c1r, c1s, net


def board_subnet(shared_dense, num_activations1, num_activation2):
    b0r, b0s, board0 = card_subnet(shared_dense, "board0")
    b1r, b1s, board1 = card_subnet(shared_dense, "board1")
    b2r, b2s, board2 = card_subnet(shared_dense, "board2")
    b3r, b3s, board3 = card_subnet(shared_dense, "board3")
    b4r, b4s, board4 = card_subnet(shared_dense, "board4")

    board = keras.layers.concatenate([board0, board1, board2, board3, board4])
    hidden = keras.layers.Dense(num_activations1, activation='relu')(board)
    net = keras.layers.Dense(num_activation2, activation='relu')(hidden)

    return b0r, b0s, b1r, b1s, b2r, b2s, b3r, b3s, b4r, b4s, net


def actions_subnet(num_act):
    inputs = keras.layers.Input(shape=(ACTIONS_INPUT_LEN,), dtype='float32', name="actions")

    x = keras.layers.Dense(num_act, activation='relu')(inputs)
    x = keras.layers.Dense(num_act/2, activation='relu')(x)
    net = keras.layers.Dense(num_act/4, activation='relu')(x)

    return inputs, net


def create_model_1():

    card_shared_dense = keras.layers.Dense(32, activation='relu')

    c0r, c0s, c1r, c1s, hc_net = holecards_subnet(card_shared_dense, 32)
    b0r, b0s, b1r, b1s, b2r, b2s, b3r, b3s, b4r, b4s, board_net = board_subnet(card_shared_dense, 64, 32)
    actions, actions_net = actions_subnet(128)

    pot_size = keras.layers.Input(shape=(1,), dtype='float32', name="pot_size")
    ammount_to_call = keras.layers.Input(shape=(1,), dtype='float32', name="ammount_to_call")
    stack_size = keras.layers.Input(shape=(1,), dtype='float32', name="stack_size")
    next_action = keras.layers.Input(shape=(3,), dtype='float32', name="action_to_play")

    hidden = keras.layers.concatenate([hc_net, board_net, actions_net, pot_size, ammount_to_call, stack_size, next_action])

    hidden = keras.layers.Dense(256, activation='relu')(hidden)
    hidden = keras.layers.Dense(64, activation='relu')(hidden)
    output = keras.layers.Dense(1, activation='linear')(hidden)

    model = keras.Model(inputs=
        [c0r, c0s, c1r, c1s, b0r, b0s, b1r, b1s, b2r, b2s, b3r, b3s, b4r, b4s, actions, pot_size, ammount_to_call, stack_size, next_action], outputs=output)

    model.compile(loss='mean_squared_error', optimizer='adam') # rmsprop, sgd
    return model