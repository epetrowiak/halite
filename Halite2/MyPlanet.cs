using Halite2.hlt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite2
{
    public class MyPlanet
    {
        public Planet Planet { get; set; }
        
        public List<int> DockingShips { get; set; }

        public MyPlanet(Planet planet)
        {
            Planet = planet;
            DockingShips = new List<int>();
        }

        public void ClearShips()
        {
            DockingShips.Clear();
        }

        public void CompleteDocking()
        {
            for(int i = 0; i < DockingShips.Count; i++)
            {
                var shipId = DockingShips[i];
                if (Planet.GetDockedShips().Contains(shipId))
                {
                }
            }
        }

    }
}
