namespace G5.Logic
{
    public class PlayerKey
    {
        public string PlayerName { get; set; }
        public PokerClient Client { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            if (this == obj)
                return true;

            var otherKey = (PlayerKey)obj;
            return otherKey.PlayerName == PlayerName && otherKey.Client == Client;
        }

        public override int GetHashCode()
        {
            if (PlayerName == null)
                return 0;

            return PlayerName.GetHashCode() ^ Client.GetHashCode();
        }
    }
}
