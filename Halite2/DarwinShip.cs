using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    public class DarwinShip : AbstractShip
    {

        public DarwinShip(Ship ship) : base(ship)
        {
        }

        public override Move DoWork()
        {
            var gm = GameMaster.Instance;
            if (Me.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return null;
            }

            var tasks = new Task[2];

            //Check Unclaimed Planet move
            var unClaimedPlanetMove = Task.Run(() => BestMoveToUnclaimedPlanet(gm));
            tasks[0] = unClaimedPlanetMove;

            //Check Claimed Planet move
            var claimedPlanetMove = Task.Run(() => BestMoveToClaimedPlanet(gm));
            tasks[1] = claimedPlanetMove;
            Task.WaitAll(tasks);

            SmartMove myMove = EvaluateBestMethod(claimedPlanetMove.Result, unClaimedPlanetMove.Result);

            //Defend My Planets
//            BestMoveToDefendMyPlanet(gm);

            //Check Move based on game state
            //            nextMove = BestGameMove(gm);
//            EvaluateBestMethod(nextMove);


            return myMove?.Move;
        }

        private SmartMove BestMoveToDefendMyPlanet(GameMaster gm)
        {
            SmartMove bestMove = null;
            var prevDist = double.MaxValue;
            var bufferDistance = 1;
            foreach (var enemyShip in gm.EnemyShipsNearMyPlanets)
            {
                var distToTarget = Me.GetDistanceTo(enemyShip);
                if (distToTarget > prevDist + bufferDistance)
                {
                    continue;
                }

                prevDist = distToTarget;
                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));
                if (move == null)
                {
                    continue;
                }

                move.Value = DefendPlanetValue(move.Value);
                bestMove = EvaluateBestMethod(move, bestMove);
            }

            return bestMove;
        }

        private SmartMove BestGameMove(GameMaster gm)
        {
            //TODO: GameStates aren't set, and unsure of how to combine this with other 2 moves
            SmartMove move = new SmartMove(double.MinValue, null);
            if (gm.GameState == GameState.Winning)
            {

            }
            else if (gm.GameState == GameState.Balanced)
            {
                
            }

            return move;
        }

        private SmartMove BestMoveToClaimedPlanet(GameMaster gm)
        {
            SmartMove bestMove = null;
            foreach (var claimedPlanet in gm.ClaimedPlanets)
            {
                SmartMove move;
                //Do nothing to my planet
                if (claimedPlanet.GetOwner() == gm.MyPlayerId)
                {
                    move = DockToMyPlanetMove(gm, claimedPlanet);
                }
                else
                {
                    move = ClaimEnemyPlanetMove(gm, claimedPlanet);
                }
                
                bestMove = EvaluateBestMethod(move, bestMove);
            }

            return bestMove;
        }

        private SmartMove DockToMyPlanetMove(GameMaster gm, Planet claimedPlanet)
        {
            if (claimedPlanet.IsFull())
            {
                return null;
            }

            return TryDockToPlanet(claimedPlanet);
        }


        private SmartMove ClaimEnemyPlanetMove(GameMaster gm, Planet enemyPlanet)
        {
            List<Task> tasks = new List<Task>();
            var moveList = new ConcurrentStack<SmartMove>();
            foreach (var shipId in enemyPlanet.GetDockedShips())
            {
                var task = Task.Run(() =>
                {
                    var enemyShip = gm.GameMap.GetShip(enemyPlanet.GetOwner(), shipId);
                    moveList.Push(NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip)));
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            SmartMove bestMove = null;
            foreach (var move in moveList)
            {
                if (move == null)
                {
                    continue;
                }

                move.Value = ClaimedPlanetMultiplier(gm, move.Value);
                bestMove = EvaluateBestMethod(move, bestMove);
            }

            return bestMove;
        }

        private SmartMove BestMoveToUnclaimedPlanet(GameMaster gm)
        {
            SmartMove bestMove = null;
            foreach (var unClaimedPlanet in gm.UnClaimedPlanets)
            {
                var dockPlanetMove = TryDockToPlanet(unClaimedPlanet);
                if (dockPlanetMove != null)
                {
                    bestMove = EvaluateBestMethod(dockPlanetMove, bestMove);
                    break;
                }

                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(unClaimedPlanet));

                if (move == null)
                {
                    continue;
                }

                move.Value = UnclaimedPlanetMultiplier(move.Value);
                bestMove = EvaluateBestMethod(move, bestMove);
            }

            return bestMove;
        }

        private SmartMove TryDockToPlanet(Planet planet)
        {
            if (!Me.CanDock(planet))
            {
                return null;
            }
            
            var curMove = new SmartMove(GetDockValue(Me, planet), new DockMove(Me, planet));
            return curMove;
        }

    }

}