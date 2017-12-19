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

        public int MyPlayerId { get; set; }

        public GameMap GameMap { get; set; }
        public GameState GameState { get; set; }
        
//        public ConcurrentStack<DarwinShip> PreviousShips { get; set; }
        
        public List<Planet> ClaimedPlanets { get; set; }
        public List<Planet> UnClaimedPlanets { get; set; }
        public List<Ship> EnemyShips { get; set; }
        
            
        public void UpdateGame(Metadata metadata)
        {
            GameMap.UpdateMap(metadata);
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
            EnemyShips.AddRange(GameMap.GetAllShips().Where(ship => ship.GetOwner() != MyPlayerId));

            //Always keep 1 battle ship
            Ship myship;
            var shipExists = GameMap.GetMyPlayer().GetShips().TryGetValue(CurrentBattleShipId, out myship);
            if(!shipExists || (myship != null && myship.GetHealth() <= 0))
            {
                KeyValuePair<int, Ship> lastUndockedShip = GameMap.GetMyPlayer().GetShips()
                    .LastOrDefault(x => x.Value.GetDockingStatus() == Ship.DockingStatus.Undocked);
                CurrentBattleShipId = lastUndockedShip.Key;
            }
        }

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