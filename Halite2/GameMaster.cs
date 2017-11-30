using System;
using System.Collections;
using System.Collections.Generic;
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


        public GameMap GameMap { get; set; }
        public GameState GameState { get; set; }
        public int MyShipCount { get; set; }
        

        public void UpdateState()
        {
            //TODO: Also track my planets and ships. And oppponents'
            //TODO: Determine my state & updates
        }

        public List<Move> PlayTurn(Metadata metadata)
        {
            //New Turn
            var moveList = new List<Move>();
            UpdateGame(metadata);

            //

            //End Turn
            return moveList;
        }

        private void UpdateGame(Metadata metadata)
        {
            GameMap.UpdateMap(metadata);
            UpdateState();
        }
    }

    public enum GameState
    {
        Expand,
        Balanced,
        Winning
    }
}