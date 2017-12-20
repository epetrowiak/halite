using System;
using System.Collections;
using System.Collections.Concurrent;
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
            CurrentBattleShipId = 2;

            ClaimedPlanets = new List<Planet>();
            UnClaimedPlanets = new List<Planet>();
            EnemyShips = new List<Ship>();
            EnemyShipsNearMyPlanets = new List<Ship>();
//            EnemyShipsWithinDockingDistance = new Dictionary<Planet, List<Ship>>();
//            PreviousShips = new ConcurrentStack<DarwinShip>();
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


        protected static readonly double _planetDefDist = 6;

        public int MyPlayerId { get; set; }

        public GameMap GameMap { get; set; }
        public GameState GameState { get; set; }
        
//        public ConcurrentStack<DarwinShip> PreviousShips { get; set; }
        
        public List<Planet> ClaimedPlanets { get; set; }
        public List<Planet> UnClaimedPlanets { get; set; }
        public List<Ship> EnemyShips { get; set; }
        public List<Ship> EnemyShipsNearMyPlanets { get; set; }

        public bool HasSatisfactoryProduction { get; set; }
        public int NumberOfMyDockedShips { get; set; }
        public int NumberOfEnemyDockedShips { get; set; }
        public int Turn { get; set; }
            
        public void UpdateGame(Metadata metadata)
        {
            GameMap.UpdateMap(metadata);
            Turn++;
            UpdateState();
        }

        public ISmartShip Activate(Ship ship)
        {
            if (IsBattleShip(ship))
            {
                return new BattleShip(ship);
            }

            return new DarwinShip(ship);
        }

        #region Update Game Helpers

        private void UpdateState()
        {
            //            PreviousShips.Clear();
            UpdatePlanets();
            UpdateShips();
        }

        private void UpdateShips()
        {
            EnemyShips.Clear();
            EnemyShips.AddRange(GameMap.GetAllShips().AsParallel().Where(ship => ship.GetOwner() != MyPlayerId));

            foreach (var myPlanet in ClaimedPlanets.Where(p => p.GetOwner() == MyPlayerId))
            {
                ParallelQuery<Ship> closeShips = EnemyShips.AsParallel().Where(s => s.GetDistanceTo(s.GetClosestPoint(myPlanet)) <= _planetDefDist);
                EnemyShipsNearMyPlanets.AddRange(closeShips);
            }

            //Always keep 1 battle ship
            Ship myship;
            var shipExists = GameMap.GetMyPlayer().GetShips().TryGetValue(CurrentBattleShipId, out myship);
            if(!shipExists || (myship != null && myship.GetHealth() <= 0) || (myship != null && myship.GetDockingStatus() != Ship.DockingStatus.Undocked))
            {
                KeyValuePair<int, Ship> lastUndockedShip = GameMap.GetMyPlayer().GetShips()
                    .LastOrDefault(x => x.Value.GetDockingStatus() == Ship.DockingStatus.Undocked);
                CurrentBattleShipId = lastUndockedShip.Key;
            }
        }

        private void UpdatePlanets()
        {
            //Reset lists
            NumberOfMyDockedShips = 0;
            NumberOfEnemyDockedShips = 0;
            ClaimedPlanets.Clear();
            UnClaimedPlanets.Clear();
            //            EnemyShipsWithinDockingDistance.Clear();

            foreach (var pair in GameMap.GetAllPlanets())
            {
                var planet = pair.Value;
                if (planet.IsOwned())
                {
                    ClaimedPlanets.Add(planet);
                    if (planet.GetOwner() == MyPlayerId)
                    {
                        NumberOfMyDockedShips += planet.GetDockedShips().Count;
                    }
                    else
                    {
                        NumberOfEnemyDockedShips += planet.GetDockedShips().Count;
                    }
                }
                else
                {
                    UnClaimedPlanets.Add(planet);
                }
            }

            //Determine if production is acceptable to begin attacking
            HasSatisfactoryProduction = NumberOfMyDockedShips + 2 >= NumberOfEnemyDockedShips
                || NumberOfMyDockedShips > 5 && Turn <= 40;
        }


        #endregion

        public int CurrentBattleShipId { get; set; }
        private bool IsBattleShip(Ship ship)
        {
            return ship.GetId() == CurrentBattleShipId;
        }
    }

    public enum GameState
    {
        Expand,
        Balanced,
        Winning,
    }
}