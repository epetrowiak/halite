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
            ClaimedPlanets = new List<Planet>();
            UnClaimedPlanets = new List<Planet>();
//            EnemyShipsWithinDockingDistance = new Dictionary<Planet, List<Ship>>();
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
        
        public List<DarwinShip> PreviousShips { get; set; }
        
        public List<Planet> ClaimedPlanets { get; set; }
        public List<Planet> UnClaimedPlanets { get; set; }
        
            
        public void UpdateGame(Metadata metadata)
        {
            GameMap.UpdateMap(metadata);
            UpdateState();
        }

        private void UpdateState()
        {
            PreviousShips.Clear();
            UpdatePlanets();
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
        
    }

    public enum GameState
    {
        Expand,
        Balanced,
        Winning,
    }
}