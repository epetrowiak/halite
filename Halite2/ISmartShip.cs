using Halite2.hlt;

namespace Halite2
{
    public interface ISmartShip
    {
        Ship Me { get; }
        Position Target { get; set; }

        Move DoWork();
        Move ReactToMyShip(ISmartShip otherShip);
    }
}