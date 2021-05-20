./Build.sh
cp 3rdParty/AcpcServer/* bin/
cd bin
./play_match.pl BotTestMatchHU holdem.nolimit.2p.reverse_blinds.game 100 0 Hero ./startme.sh Villian ./example_player.nolimit.2p.sh
