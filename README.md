# g5-poker-bot

AI algorithm for playing texas holdem poker

## Introduction

G5 is poker playing program (bot) which won first place at [Annual Computer Poker Competition](http://www.computerpokercompetition.org/) 2018 in Six-Player No-Limit Texas Hold'em category and second place at Acpc 2017 in Heads-up No-Limit Texas Hold'em category.

## Quick Links

 * Acpc 2017 results: http://www.computerpokercompetition.org/downloads/competitions/2017/xtable/
 * Acpc 2018 results: http://www.computerpokercompetition.org/index.php/competitions/results/128-2018-results

## Building

G5 is written in C++ and C#. Decision making logic is in C++, while Acpc client and program to play against the bot is written in C#. Visual Studio 2017 is required to build and run the program on Windows. While building Acpc client requires .NET core build chain for Linux (Tested in Ubuntu 16.04).

## Playing Logic

Playing logic is based on Bayesian opponent modeling, so it can figure out approximate playing style of opponent from only a few hands. Initial (prior) playing strategy of the opponent is estimated from hand histories from internet play (2 million hands used). After each played hand bot updates the model using Bayesian inference.

Opponent model is then used in standard [Expectiminimax](https://en.wikipedia.org/wiki/Expectiminimax) or Miximax search through game tree, where opponent range is also estimated using Bayesian inference as hand progresses. At opponent nodes, algorithm considers all possible actions with probabilities given by opponent model.  At leaf nodes expected value is calculated using estimated opponent range.

## Recent Developments and Resources

* Intermission - wining bot in 2017 competition: http://www.unfoldpoker.com/intermission-2017/
* Ruse: https://www.ruse.ai/news/ruse-vs-slumbot
* Slumbot: https://www.slumbot.com/#
* Supremus: https://arxiv.org/pdf/2007.10442.pdf
* ReBel: https://arxiv.org/pdf/2007.13544.pdf
* Safe and Nested Subgame Solving: http://www.cs.cmu.edu/~noamb/papers/17-NIPS-Safe.pdf

## License

G5 is released under MIT license.
