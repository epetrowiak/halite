using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public class GameMaster
    {
        #region Singleton (sorta)
        private static GameMaster _instance { get; set; }

        public static GameMaster Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new Exception("Forgot to initialize the GameMaster");
                }
                return _instance;
            }
        }

        private GameMaster(GameMap map)
        {
            GameMap = map;
            GameState = GameState.Expand;
            MyPlayerId = GameMap.GetMyPlayerId();
            MyShips = new Dictionary<int, DarwinShip>();
            ClaimedPlanets = new List<Planet>();
            UnClaimedPlanets = new List<Planet>();
//            EnemyShipsWithinDockingDistance = new Dictionary<Planet, List<Ship>>();
            Leader = GameMap.GetMyPlayer();
            PreviousShips = new List<DarwinShip>();
        }

        public static GameMaster Initialize(GameMap gameMap)
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new GameMaster(gameMap);
            return _instance;
        }
        #endregion

        public int MyPlayerId { get; set; }

        public GameMap GameMap { get; set; }
        public GameState GameState { get; set; }
        
        public Dictionary<int, DarwinShip> MyShips { get; set; }
        public List<DarwinShip> PreviousShips { get; set; }
        public double MyShipsOverEnemyPercentage { get; set; }

        public bool IsAllPlanetsOwned => ClaimedPlanets.Any() && !UnClaimedPlanets.Any();
        public List<Planet> ClaimedPlanets { get; set; }
        public List<Planet> UnClaimedPlanets { get; set; }

        public Player Leader { get; set; }
        public int TurnCount { get; set; }
        
//        public Dictionary<Planet, List<Ship>> EnemyShipsWithinDockingDistance { get; set; }

            
        public void UpdateGame(Metadata metadata)
        {
            GameMap.UpdateMap(metadata);
            UpdateState();
            TurnCount++;
        }

        private void UpdateState()
        {
//            UpdateMyShips();
            PreviousShips.Clear();
            UpdatePlanets();
//            UpdateLeader();
        }

        private void UpdateLeader()
        {
            var totalShips = 0;
            var leaderShipCount = Leader.GetShips().Count(s => s.Value.GetHealth() > 0);
            var leaderPlanetCount = ClaimedPlanets.Count(p => p.GetOwner() == Leader.GetId());
            foreach (var player in GameMap.GetAllPlayers())
            {
                var shipCount = GetPlayerShipCount(player);
                totalShips += shipCount;

                var planetCount = ClaimedPlanets.Count(p => p.GetOwner() == player.GetId());
                if (leaderShipCount < shipCount && leaderPlanetCount <= planetCount)
                {
                    Leader = player;
                    leaderShipCount = shipCount;
                    leaderPlanetCount = planetCount;
                }
            }

            MyShipsOverEnemyPercentage = (double)MyShips.Count / totalShips;
        }

        private int GetPlayerShipCount(Player player)
        {
//            int shipCount = 0;
//            if (player.GetId() != MyPlayerId)
//            {
//                foreach (var ship in player.GetShips().Values)
//                {
//                    if (ship.GetHealth() <= 0)
//                    {
//                        continue;
//                    }
//
//                    EnemyNearPlanet(ship);
//
//                    shipCount++;
//                }
//            }
//            else
//            {
//                shipCount = MyShips.Count;
//            }
            return player.GetId() == MyPlayerId ? MyShips.Count :
                player.GetShips().Values.Count(ship => ship.GetHealth() > 0);
        }

//        private void EnemyNearPlanet(Ship ship)
//        {
//            foreach (var planet in GameMap.GetAllPlanets().Values)
//            {
//                if (ship.GetDistanceTo(planet) > planet.GetRadius() + Constants.DOCK_RADIUS)
//                {
//                    continue;
//                }
//
//                if (EnemyShipsWithinDockingDistance.ContainsKey(planet))
//                {
//                    EnemyShipsWithinDockingDistance[planet].Add(ship);
//                }
//                else
//                {
//                    EnemyShipsWithinDockingDistance.Add(planet, new List<Ship>(){ship});
//                }
//            }
//        }

        private void UpdatePlanets()
        {
            //Reset lists
            ClaimedPlanets.Clear();
            UnClaimedPlanets.Clear();
//            EnemyShipsWithinDockingDistance.Clear();

            foreach (var pair in GameMap.GetAllPlanets())
            {
                var planet = pair.Value;
                if (planet.IsOwned())
                {
                    ClaimedPlanets.Add(planet);
                }
                else
                {
                    UnClaimedPlanets.Add(planet);
                }
            }
        }

        private void UpdateMyShips()
        {
            var shipCatalog = GameMap.GetMyPlayer().GetShips();
            foreach (var pair in shipCatalog)
            {
                if (!MyShips.ContainsKey(pair.Key))
                {
                    MyShips.Add(pair.Key, ActivateShip(pair.Value));
                }
                else
                {
                    if (pair.Value.GetHealth() <= 0)
                    {
                        MyShips.Remove(pair.Key);
                    }
                }
            }
        }

        private DarwinShip ActivateShip(Ship ship)
        {
            var darwin = new DarwinShip(ship);

            return darwin;
        }
    }

    public enum GameState
    {
        Expand,
        Balanced,
        Winning,
    }
}