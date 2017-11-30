using Halite2.hlt;

namespace Halite2
{
    public class DarwinShip
    {
        public GameMap Map { get; }
        public ShipType ShipType { get; set; }
        public Ship Me { get; }


        public DarwinShip(GameMaster ga, Ship ship, ShipType st)
        {
            Map = ga.GameMap;
            Me = ship;
            ShipType = st;
        }

        public Move DoWork()
        {
            return new ThrustMove(Me, 1, 1);
        }
    }

    public enum ShipType
    {
        Miner,
        Battle
    }
}