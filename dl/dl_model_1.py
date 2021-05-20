import tensorflow as tf
from tensorflow import keras
import numpy as np
import action_helpers as ah
from random import randint


# 4 streets, 5 actions per street, 2 (br or cc...)
ACTIONS_INPUT_LEN = 4*5*2


def card_subnet(shared_dense, id):
    input_rank = keras.layers.Input(shape=(ah.NUM_RANKS,), dtype='float32', name=id + "_rank")
    input_suite = keras.layers.Input(shape=(ah.NUM_SUITES,), dtype='float32', name=id + "_suite")

    input_card = keras.layers.concatenate([input_rank, input_suite])
    net  = shared_dense(input_card)

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


def onehot(indices, count):
    oneh = np.zeros((len(indices), count))

    for i in range(len(indices)):
        if indices[i] >= 0:
            oneh[i][indices[i]] = 1

    return oneh


def vals_to_np(vals):
    res = np.zeros((len(vals), 1))

    for i in range(len(vals)):
        res[i][0] = vals[i]

    return res


def cards_to_input(inputs, cards):
    rank_input = onehot([card.rank for card in cards], ah.NUM_RANKS)
    suite_input = onehot([card.suite for card in cards], ah.NUM_SUITES)

    inputs.append(rank_input)
    inputs.append(suite_input)

    #print(rank_input.shape)
    #print(suite_input.shape)


def actions_to_input(inputs, all_actions):
    res = np.zeros((len(all_actions), ACTIONS_INPUT_LEN))

    for j in range(len(all_actions)):
        actions = all_actions[j]

        for i in range(20):
            if actions[i].type == "cc":
                res[j][2*i + 0] = 1
            elif actions[i].type == "br":
                res[j][2*i + 1] = 1
    
    #print(res.shape)
    inputs.append(res)


def gym_states_to_inputs(states, actions):
    inputs = []

    cards_to_input(inputs, [st.hole_cards.card0 for st in states])
    cards_to_input(inputs, [st.hole_cards.card1 for st in states])

    for i in range(5):
        cards_to_input(inputs, [st.board[i] for st in states])

    actions_to_input(inputs, [st.actions for st in states])
    
    pot_size = vals_to_np([st.pot_size for st in states])
    #print("Pot size: ", pot_size.shape)

    ammount_to_call = vals_to_np([st.ammount_to_call for st in states])
    #print("Ammount to call: ", ammount_to_call.shape)

    stack_size = vals_to_np([st.stack_size for st in states])
    #print("Stack size: ", stack_size.shape)

    act_inputs = onehot(actions, 3)
    #print("Actions: ", act_inputs.shape)

    inputs.append(pot_size)
    inputs.append(ammount_to_call)
    inputs.append(stack_size)
    inputs.append(act_inputs)

    return inputs


def calculate_action(model, state, random_odds):
    
    rnd = randint(0, 99)
    print("Calulating action. Rnd: ", rnd, random_odds)

    if rnd < random_odds:
        return ah.randomize_action(state.pot_size, state.ammount_to_call, state.stack_size)

    inputs = gym_states_to_inputs([state, state, state], [0, 1, 2])
    predictions = model.predict(inputs)

    return ah.calculate_action(predictions, state.pot_size, state.ammount_to_call, state.stack_size)
    

def train_model(model, states, actions, winnings, batch_size, validation_split, epochs):

    inputs = gym_states_to_inputs(states, actions)
    outputs = vals_to_np(winnings)

    model.fit(inputs, outputs, batch_size=batch_size, validation_split=validation_split, epochs=epochs)


#model = create_model_1()
