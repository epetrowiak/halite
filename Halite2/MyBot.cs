using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    public class MyBot
    {

        public static void Main(string[] args)
        {
            Networking networking = new Networking();
            var gameMaster = GameMaster.Initialize(networking.Initialize("Meucci"));

            var moveStack = new ConcurrentStack<Move>();
            while(true)
            {
                moveStack.Clear();
                var readLineIntoMetadata = Networking.ReadLineIntoMetadata();
                gameMaster.UpdateGame(readLineIntoMetadata);
                var gameMap = gameMaster.GameMap;

                Parallel.ForEach(gameMap.GetMyPlayer().GetShips(), kvp =>
                {
                    var ship = kvp.Value;
                    if (ship.GetHealth() <= 0) //Dunno if these are tracked
                    {
                        return;
                    }

                    var myShip = gameMaster.Activate(ship);

                    var bestMove = myShip.DoWork();
                    if (bestMove != null)
                    {
                        moveStack.Push(bestMove);
                    }
                });

                Networking.SendMoves(moveStack);
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
