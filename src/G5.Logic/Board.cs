using System.Collections.Generic;
using System.Diagnostics;

namespace G5.Logic
{
    public class Board
    {
        public List<Card> Cards { get; private set; }

        public Board()
        {
            Cards = new List<Card>();
        }

        public Board(Board other)
        {
            Cards = new List<Card>();

            foreach (var card in other.Cards)
                Cards.Add(card);
        }

        public Board(string stringRepresentation)
        {
            Cards = new List<Card>();

            if (stringRepresentation != null)
            {
                for (var i = 0; i <= stringRepresentation.Length - 2; i += 2)
                {
                    var cardStr = stringRepresentation.Substring(i, 2);
                    var card = new Card(cardStr);

                    if (card.rank != Card.Rank.Unknown && card.suite != Card.Suite.Unknown)
                    {
                        Cards.Add(card);
                    }
                }
            }
        }

        public Card[] getSortedCards()
        {
            if (Cards.Count == 0)
                return new Card[0];

            Card[] sortedCards = Cards.ToArray();

            for (int i = 0; i < Cards.Count-1; i++)
            {
                for (int j = i + 1; j < Cards.Count; j++)
                {
                    if (sortedCards[i].rank < sortedCards[j].rank)
                    {
                        Card tmp = sortedCards[i];
                        sortedCards[i] = sortedCards[j];
                        sortedCards[j] = tmp;
                    }
                }
            }

            return sortedCards;
        }

        public int Count
        {
            get
            {
                return Cards.Count;
            }
        }

        public List<Card> Flop
        {
            get
            {
                Debug.Assert(Count >= 3);

                return (Count >= 3)
                           ? Cards.GetRange(0, 3)
                           : null;
            }
        }

        public Card Turn
        {
            get
            {
                Debug.Assert(Count > 3);

                return (Cards.Count > 3)
                           ? Cards[3]
                           : new Card();
            }
        }

        public Card River
        {
            get
            {
                Debug.Assert(Count > 4);

                return (Cards.Count > 4)
                           ? Cards[4]
                           : new Card();
            }
        }


        public void AddCard(Card card)
        {
            Cards.Add(card);
            Debug.Assert(Count <= 5);
        }

        public override string ToString()
        {
            return string.Join(string.Empty, Cards);
        }

        public Card[] ToArray()
        {
            return Cards.ToArray();
        }
    }
}
