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

                var myShips = gameMap.GetMyPlayer().GetShips();
                

//                Starting scenario
                if (myShips.Count == 3)
                {
                    var moves = GetStarterMoves(myShips, gameMaster, gameMap);
                    foreach (var move in moves)
                    {
                        if (move == null)
                        {
                            continue;
                        }

                        moveStack.Push(move);
                    }
                }
                else
                {
                    Parallel.ForEach(myShips, kvp =>
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
                }

                Networking.SendMoves(moveStack);
            }
        }

        private static Move[] GetStarterMoves(IDictionary<int, Ship> myShips, GameMaster gameMaster, GameMap gameMap)
        {
            var starterShips = new ISmartShip[3];
            var moves = new Move[3];
            int x = 0;
            foreach (var kvp in myShips)
            {
                var myShip = gameMaster.Activate(kvp.Value);
                starterShips[x] = myShip;
                var bestMove = myShip.DoWork();

                if (bestMove != null)
                {
                    moves[x] = bestMove;
                }
                x++;
            }

            for (int i = 0; i < starterShips.Length; i++)
            {
                var iShip = starterShips[i];
                for (int j = i + 1; j < starterShips.Length; j++)
                {
                    var jShip = starterShips[j];
                    if (!IsShipsAboutToCrash(iShip, jShip))
                    {
                        continue;
                    }
                    
                    moves[i] = ChangeMove(gameMap, iShip, jShip);
                    moves[j] = ChangeMove(gameMap, jShip, iShip);
                }
            }

            return moves;
        }
        
        private static Move ChangeMove(GameMap gameMap, ISmartShip ship, ISmartShip otherShip)
        {
            if (ship.Target == null || ship.Me == null
                || otherShip.Target == null || otherShip.Me == null)
            {
                return null;
            }
            return ship.ReactToMyShip(otherShip);
        }

        private static bool IsShipsAboutToCrash(ISmartShip ship, ISmartShip other)
        {
            var dx = Math.Abs(ship.Me.GetXPos() - other.Me.GetXPos());
            var dy = Math.Abs(ship.Me.GetYPos() - other.Me.GetYPos());
            return dx <= ship.Me.GetRadius() *2 && dy <= ship.Me.GetRadius()*2;
        }

        private static void SetupGame(string[] args)
        {
            Networking networking = new Networking();
            GameMap gameMap = networking.Initialize("Meucci");
            GameMaster.Initialize(gameMap);
        }
    }
}
