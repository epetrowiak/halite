using System;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public class MyBot
    {
        public static void Main(string[] args)
        {
            Networking networking = new Networking();
            var gameMaster = GameMaster.Initialize(networking.Initialize("Meucci"));

            List<Move> moveList = new List<Move>();
            while(true)
            {
                moveList.Clear();
                var readLineIntoMetadata = Networking.ReadLineIntoMetadata();
                gameMaster.UpdateGame(readLineIntoMetadata);
                var gameMap = gameMaster.GameMap;
                
                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    var myShip = new DarwinShip(ship);
                    //Ship 1 will immediately attack
                    if (ship.GetId() == 1)
                    {
                        var doBattleWithNearestEnemy = myShip.DoBattleWithNearestEnemy();
                        if (doBattleWithNearestEnemy != null)
                        {
                            moveList.Add(doBattleWithNearestEnemy);
                        }
                        continue;
                    }


                    var bestMove = myShip.DoWork();

                    if (bestMove != null)
                    {
                        moveList.Add(bestMove);
                    }
                    gameMaster.PreviousShips.Add(myShip);
                }

                Networking.SendMoves(moveList);
            }
        }
        

        private static void SetupGame(string[] args)
        {
            Networking networking = new Networking();
            GameMap gameMap = networking.Initialize("Meucci");
            GameMaster.Initialize(gameMap);
        }
    }
}
