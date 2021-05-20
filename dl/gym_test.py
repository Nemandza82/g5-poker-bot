import time
from load_gym import load_gym
import action_helpers as ah
import dl_model_1 as m1


def append_winnings(all_states, all_winnings, winnings):
    while len(all_winnings) < len(all_states):
        id = len(all_winnings)
        player_id = all_states[id].player_to_act
        all_winnings.append(winnings[player_id])


def play_hand(gym, model0, rnd_odds0, model1, rnd_odds1, what_if_play):
    state = gym.startHand()
    
    all_states = []
    all_actions = []
    all_winnings = []

    hand_saldo = []

    while state.status != "hand_finished":

        if state.player_to_act == 0:
            [action, action_ind], ammount = m1.calculate_action(model0, state, rnd_odds0)
        else:
            [action, action_ind], ammount = m1.calculate_action(model1, state, rnd_odds1)

        is_fake_action = False

        # In case of fold we can continue playing...
        if (action == ah.ACTION_FOLD[0]) and what_if_play:
            
            print("Player:", state.player_to_act, "wanted to fold - randomizing action ******")

            winn = [0, 0]
            winn[state.player_to_act] = 0
            winn[state.other_player_ind] = state.pot_size

            print("Winnings:", winn)
            append_winnings(all_states, all_winnings, winn)

            if len(hand_saldo) == 0:
                hand_saldo = [0, 0]
                hand_saldo[state.player_to_act] = state.stack_size - state.start_stack_size
                hand_saldo[state.other_player_ind] = -hand_saldo[state.player_to_act]

            print("Hand saldo at the moment of first fold:", hand_saldo)

            # randomize new action and continue playing...
            [action, action_ind], ammount = ah.randomize_action(state.pot_size, state.ammount_to_call, state.stack_size, never_fold=True)
            is_fake_action = True
        
        if state.player_to_act == 0:
            all_states.append(state)
            all_actions.append(action_ind)

        print("Calculated action:", action, ammount)
        state = gym.act(action, ammount, is_fake_action)

    append_winnings(all_states, all_winnings, state.winnings)
    print("All winings:", all_winnings)

    if len(hand_saldo) == 0:
        hand_saldo = [state.saldo[0], state.saldo[1]]
        print("Taking state saldo ----")

    print("Final hand saldo:", [state.saldo[0], state.saldo[1]])
    print("Returned hand saldo:", hand_saldo)

    return all_states, all_actions, all_winnings, hand_saldo


def play_manu_hands(gym, model0, rnd_odds0, model1, rnd_odds1, num_hands, what_if_play):
    all_states = []
    all_actions = []
    all_winnings = []

    total_saldo = [0, 0]

    for i in range(num_hands):
        print("")
        print("Hand: ", i)
        states, actions, winnings, saldo = play_hand(gym, model0, rnd_odds0, model1, rnd_odds1, what_if_play)

        total_saldo[0] += saldo[0]
        total_saldo[1] += saldo[1]

        print("Avg saldo per hand:", round(total_saldo[0] / (i + 1), 2), ",", round(total_saldo[1] / (i + 1), 2))

        for st in states:
            all_states.append(st)

        for act in actions:
            all_actions.append(act)

        for winn in winnings:
            all_winnings.append(winn)

    total_saldo[0] /= num_hands
    total_saldo[1] /= num_hands

    print("")
    print("Bot 0 score: ", total_saldo[0], "per hand")
    print("Bot 1 score: ", total_saldo[1], "per hand")

    print("")
    print("Colected ", len(all_states), " data pairs for training.")

    return all_states, all_actions, all_winnings, total_saldo


def load_opp_models(model_paths, rnd_odds):
    models = []
    opp_names = []

    for i in range(len(model_paths)):
        opp_model = m1.create_model_1()
        opp_model.load_weights(model_paths[i])

        models.append(opp_model)

        if rnd_odds[i] == 100:
            opp_names.append("random")
        else:
            opp_names.append(model_paths[i])

    return models, rnd_odds, opp_names


gym = load_gym()
f = open("log.txt", "w")

training_model = m1.create_model_1()
training_model.load_weights("weights0012.h5")
training_model_rnd_odds = 5

#opp_models, rnd_odds, opp_name = load_opp_models(["model_1_lvl_00.h5", "model_1_lvl_00.h5", "model_1_lvl_01.h5", "model_1_lvl_02.h5"], [100, 0, 0, 0])
opp_models, rnd_odds, opp_name = load_opp_models(["model_1_lvl_01.h5"], [0])

num_iters = 50000
num_hands = 4000
what_if_play = True
do_training = True
training_epochs = 30

# Leveling params
saldo_limit_for_next_lvl = 200
next_level = 4
max_opp_models = 20

for i in range(num_iters):

    print("\nIteration:", i, "\n", file=f)
    f.flush()

    states = []
    actions = []
    winnings = []
    #saldos = []

    go_to_next_level = True

    # Play against opp models
    for j in range(len(opp_models)):
        print("Playing vs", opp_name[j], file=f)
        f.flush()
        
        start_time = time.time()
        st, act, winn, saldo = play_manu_hands(gym, training_model, training_model_rnd_odds, opp_models[j], rnd_odds[j], num_hands=num_hands, what_if_play=what_if_play)
        elapsed_time = time.time() - start_time

        states.append(st)
        actions.append(act)
        winnings.append(winn)
        #saldos.append(saldo)

        if saldo[0] < saldo_limit_for_next_lvl:
            go_to_next_level = False

        print("Played", num_hands, "hands in", round(elapsed_time), "seconds", round(1000 * elapsed_time / num_hands), "ms per hand", file=f)
        print("Saldo vs", opp_name[j], saldo, "\n", file=f)
        f.flush()

    if do_training and go_to_next_level:
        file_name = "model_1_lvl_" + str(next_level).zfill(2) + ".h5"
        print("Went to next level:", file_name, "\n", file=f)
        f.flush()

        training_model.save_weights(file_name)
        next_level += 1

        # Push training model to opponent models
        opp_models.append(training_model)
        rnd_odds.append(0)
        opp_name.append(file_name)

        if len(opp_models) > max_opp_models:
            opp_models.pop(0)
            rnd_odds.pop(0)
            opp_name.pop(0)

        # Make new training model. Continue where last one left off
        training_model = m1.create_model_1()
        training_model.load_weights(file_name)

    if do_training:
        print("Now training\n", file=f)
        f.flush()

        for j in range(len(states)):
            real_epochs = training_epochs

            #if (saldos[j][0] < 0):
            #    real_epochs *= 2

            start_time = time.time()
            m1.train_model(training_model, states[j], actions[j], winnings[j], batch_size=128, validation_split=0.1, epochs=real_epochs)
            elapsed_time = time.time() - start_time

            print("Trained", real_epochs, "epochs in", round(elapsed_time), "seconds", round(elapsed_time / real_epochs, 2), "seconds per epoch", file=f)
            f.flush()

        file_name = "weights" + str(i).zfill(4) + ".h5"
        training_model.save_weights(file_name)

        print("\nSaved weights:", file_name, file=f)
        f.flush()

f.close()