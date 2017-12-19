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
                
                foreach (var ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    if (ship.GetHealth() <= 0) //Dunno if these are tracked
                    {
                        continue;
                    }

                    var myShip = new DarwinShip(ship);
                    //Ship 2 will immediately attack
                    if (ship.GetId() == 2)
                    {
                        DoBattle(myShip, moveList);
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

        private static void DoBattle(DarwinShip myShip, List<Move> moveList)
        {
            var doBattleWithNearestEnemy = myShip.DoBattleWithNearestEnemy();
            if (doBattleWithNearestEnemy != null)
            {
                moveList.Add(doBattleWithNearestEnemy);
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
