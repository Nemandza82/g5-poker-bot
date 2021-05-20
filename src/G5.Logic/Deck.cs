using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace G5.Logic
{
    public class Deck
    {
        private Card[] _cards = new Card[52];
        private Random _rnd = new Random();
        private int _curr = 0;

        public Deck()
        {
            for (int i = 0; i < 52; i++)
                _cards[i] = new Logic.Card(i);

            reset();
        }

        private void shuffle()
        {
            for (int i = 0; i < 51; i++)
            {
                int j = _rnd.Next(i + 1, 52);

                Card tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        public Card dealCard()
        {
            Card card = _cards[_curr++];

            if (_curr == 52)
                reset();

            return card;
        }

        public HoleCards dealHoleCards()
        {
            return new HoleCards(dealCard(), dealCard());
        }

        public void reset()
        {
            shuffle();
            _curr = 0;
        }
    }
}
