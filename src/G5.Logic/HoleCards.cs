using System.Collections.Generic;
using System.Diagnostics;

namespace G5.Logic
{
    public class HoleCards
    {
        public Card Card0 { get; private set; }
        public Card Card1 { get; private set; }

        public HoleCards(int index)
        {
            Debug.Assert(index >= 0 && index <= 2703);

            int ind0 = index / 52;
            int ind1 = index % 52;

            Card0 = new Card(ind0);
            Card1 = new Card(ind1);

            SortCards();
        }

        public HoleCards(string str)
        {
            Debug.Assert(str.Length == 4);

            Card0 = new Card(str.Substring(0, 2));
            Card1 = new Card(str.Substring(2, 2));

            SortCards();
        }

        public HoleCards(Card card0, Card card1)
        {
            Card0 = card0;
            Card1 = card1;

            SortCards();
        }

        public HoleCards(List<Card> cards)
        {
            Debug.Assert(cards.Count == 2);

            Card0 = cards[0];
            Card1 = cards[1];

            SortCards();
        }

        public HoleCards(int cardIndex1, int cardIndex2)
        {
            Card0 = new Card(cardIndex1);
            Card1 = new Card(cardIndex2);

            SortCards();
        }

        public HoleCards(Card.Rank rank1, Card.Suite suite1, Card.Rank rank2, Card.Suite suite2)
        {
            Card0 = new Card(suite1, rank1);
            Card1 = new Card(suite2, rank2);

            SortCards();
        }

        private void SortCards()
        {
            if (Card0.ToInt() > Card1.ToInt())
            {
                Card tmp = Card0;
                Card0 = Card1;
                Card1 = tmp;
            }
        }

        public Card GetCard(int ind)
        {
            return (ind == 0) ? Card0 : Card1;
        }

        public int ToInt()
        {
            int ind0 = Card0.ToInt();
            int ind1 = Card1.ToInt();

            return ind0 * 52 + ind1;
        }

        public override string ToString()
        {
            return Card0.ToString() + Card1.ToString();
        }
    }
}
