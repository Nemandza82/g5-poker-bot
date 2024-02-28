using System.Collections.Generic;
using System.Diagnostics;

namespace G5.Logic
{
    /// <summary>
    /// Struktura koja opisuje jednu kartu. Sadrzi boju i rank.
    /// </summary>
    public struct Card
    {
        /// <summary>
        /// Enum koji prestavlja boju (suite) karte
        /// </summary>
        public enum Suite
        {
            Unknown = -1,
            Clubs = 0,
            Diamonds,
            Hearts,
            Spades
        };

        /// <summary>
        /// Enum koji prestavlja broj (rank) karte
        /// </summary>
        public enum Rank
        {
            Unknown = 0,
            Deuce   = 2,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine,
            Ten,
            Jack,   // 11
            Queen,  // 12
            King,   // 13
            Ace     // 14
        }

        /// <summary>
        /// Broj/rank karte
        /// </summary>
        public Rank rank { get; set; }

        /// <summary>
        /// Boja/znak/suite karte
        /// </summary>
        public Suite suite { get; set; }

        public Card(int value) : this()
        {
            Debug.Assert(value >= 0 && value <= 51);

            suite = (Suite)(value % 4);
            rank = (Rank)(14 - value / 4);
        }

        /// <summary>
        /// Formira kartu od boje i ranka
        /// </summary>
        /// <param name="aSuite">Boja buduce karte</param>
        /// <param name="aRank">Rank buduce karte</param>
        public Card(Suite aSuite, Rank aRank) : this()
        {
            suite = aSuite;
            rank = aRank;
        }

        /// <summary>
        /// Karta se formira od stringa (Jh Ad 3c)
        /// </summary>
        /// <param name="stringRepresentation">String koji predstavlja kartu (Jh Ad 3c)</param>
        public Card(string stringRepresentation) : this()
        {
            Debug.Assert(stringRepresentation != null && (stringRepresentation.Length == 2 || stringRepresentation.Length == 3));

            rank = Rank.Unknown;
            suite = Suite.Unknown;

            if (stringRepresentation.Length == 2)
            {
                char charRank = stringRepresentation[0];
                char charSuite = stringRepresentation[1];

                if (charRank == '2')
                    rank = Rank.Deuce;
                else if (charRank == '3')
                    rank = Rank.Three;
                else if (charRank == '4')
                    rank = Rank.Four;
                else if (charRank == '5')
                    rank = Rank.Five;
                else if (charRank == '6')
                    rank = Rank.Six;
                else if (charRank == '7')
                    rank = Rank.Seven;
                else if (charRank == '8')
                    rank = Rank.Eight;
                else if (charRank == '9')
                    rank = Rank.Nine;
                else if (charRank == 'T')
                    rank = Rank.Ten;
                else if (charRank == 'J')
                    rank = Rank.Jack;
                else if (charRank == 'Q')
                    rank = Rank.Queen;
                else if (charRank == 'K')
                    rank = Rank.King;
                else if (charRank == 'A')
                    rank = Rank.Ace;

                if (charSuite == 'c')
                    suite = Suite.Clubs;
                else if (charSuite == 'h')
                    suite = Suite.Hearts;
                else if (charSuite == 'd')
                    suite = Suite.Diamonds;
                else if (charSuite == 's')
                    suite = Suite.Spades;
            }
            else if (stringRepresentation.Length == 3)
            {
                string stringRank = stringRepresentation.Substring(0, 2);
                char charSuite = stringRepresentation[2];

                if (stringRank == "10")
                    rank = Rank.Ten;

                if (charSuite == 'c')
                    suite = Suite.Clubs;
                else if (charSuite == 'h')
                    suite = Suite.Hearts;
                else if (charSuite == 'd')
                    suite = Suite.Diamonds;
                else if (charSuite == 's')
                    suite = Suite.Spades;
            }

            Debug.Assert(suite != Suite.Unknown && rank != Rank.Unknown);
        }

        public static string RankToString(Rank rank)
        {
            string stringCard = null;

            if (rank == Rank.Deuce)
                stringCard = "2";
            else if (rank == Rank.Three)
                stringCard = "3";
            else if (rank == Rank.Four)
                stringCard = "4";
            else if (rank == Rank.Five)
                stringCard = "5";
            else if (rank == Rank.Six)
                stringCard = "6";
            else if (rank == Rank.Seven)
                stringCard = "7";
            else if (rank == Rank.Eight)
                stringCard = "8";
            else if (rank == Rank.Nine)
                stringCard = "9";
            else if (rank == Rank.Ten)
                stringCard = "T";
            else if (rank == Rank.Jack)
                stringCard = "J";
            else if (rank == Rank.Queen)
                stringCard = "Q";
            else if (rank == Rank.King)
                stringCard = "K";
            else if (rank == Rank.Ace)
                stringCard = "A";

            return stringCard;
        }

        private string SuiteToString()
        {
            string stringCard = null;

            if (suite == Suite.Clubs)
                stringCard += "c";
            else if (suite == Suite.Diamonds)
                stringCard += "d";
            else if (suite == Suite.Hearts)
                stringCard += "h";
            else if (suite == Suite.Spades)
                stringCard += "s";

            return stringCard;
        }

        /// <summary>
        /// Vraca string reprezentaciju karte (Tc, As, Kd, Qh ....)
        /// </summary>
        /// <returns>String reprezentaciju karte (Tc, As, Kd, Qh ....)</returns>
        override public string ToString()
        {
            return RankToString(rank) + SuiteToString();
        }

        public int ToInt()
        {
            int value = (14 - (int)rank) * 4 + (int)suite;
            Debug.Assert(value >= 0 && value <= 51);

            return value;
        }

        public dynamic ToRankSuite()
        {
            return new { rank = (int)rank - 2, suite = (int)suite };
        }

        public static List<Card> StringToCards(string stringRepresentation)
        {
            var cards = new List<Card>();

            if (stringRepresentation == null || stringRepresentation.Length < 2)
                return cards;

            for (var i = 0; i <= stringRepresentation.Length - 2; i += 2)
            {
                cards.Add(new Card(stringRepresentation.Substring(i, 2)));
            }

            return cards;
        }
    }
}
