 * Submission deadline: Friday January 13, 2017.
 * Amazon EC2 - m4.large (2 vCPU 8GB).
 * Maximum program size 250 GB.
 * Deadline for special software installation: December 1 2016.
 * Server version: 1.0.39.
 * Protocol version: 2.0.0, October 2010.
 * Reverse blinds:
   * Dealer puts in the small blind, and plays first PreFlop.
   * PostFlop, the other player bids first.
 * Time limit 7s per hand.
 * Max time per single action 10m.
 * Competitors will  be able to play hands against benchmark agents.
 * Hands Per Match:	3000
 * Stack Sizes:	200 big blinds
 * Blind Sizes:	50/100

 * Acpc server sample usage
   * Start server: ./bm_server bm_server.config
   * The fastest way to start a match:
     * ./play_match.pl BotTestMatch holdem.nolimit.2p.reverse_blinds.game 100 0 Hero ./G5.Acpc Villian ./example_player.nolimit.2p.sh

 * play_match.pl expects player executables that take exactly two arguments:
   * The server IP followed by the port number.
