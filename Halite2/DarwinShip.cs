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

            SmartMove myMove = EvaluateBestMethod(claimedPlanetMove.Result, unClaimedPlanetMove.Result) ??
                               MoveToNearestAlly(gm); //Find a friend and follow that guy :)

            //Defend My Planets

            //Check Move based on game state
            //            nextMove = BestGameMove(gm);
//            EvaluateBestMethod(nextMove);

            if (myMove != null)
            {
                Target = myMove.Target;
            }

            return myMove?.Move;
        }

        public override Move ReactToMyShip(ISmartShip otherShip)
        {
            var gm = GameMaster.Instance;
            if (Me.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return null;
            }

            SmartMove bestMove = null;
            foreach (var planet in gm.UnClaimedPlanets)
            {
                var dockPlanetMove = TryDockToPlanet(planet);
                if (dockPlanetMove != null)
                {
                    bestMove = dockPlanetMove;
                    break;
                }
            }

            if (bestMove == null)
            {
                var dx = Me.GetXPos() - otherShip.Me.GetXPos();
                var dy = Me.GetYPos() - otherShip.Me.GetYPos();
                var position = new Position(Me.GetXPos() + dx * 2, Me.GetYPos() + dy * 2);
                bestMove = NavigateToTarget(gm.GameMap, position);
            }

            return bestMove?.Move;
        }

        private SmartMove MoveToNearestAlly(GameMaster gm)
        {
            var dist = double.MaxValue;
            SmartMove curMove = null;
            foreach (var myShip in gm.GameMap.GetMyPlayer().GetShips().Values)
            {
                //Only team up with undocked ships
                if (myShip.GetDockingStatus() != Ship.DockingStatus.Undocked)
                {
                    continue;
                }

                var distanceTo = Me.GetDistanceTo(myShip);
                if (distanceTo < dist)
                {
                    dist = distanceTo;
                    SmartMove myMove = NavigateToTarget(gm.GameMap, myShip);
                    curMove = EvaluateBestMethod(myMove, curMove);
                }
            }

            return curMove;
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
            SmartMove curMove = null;
            foreach (var shipId in enemyPlanet.GetDockedShips())
            {
                var enemyShip = gm.GameMap.GetShip(enemyPlanet.GetOwner(), shipId);
                curMove = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));
                if (curMove != null)
                {
                    curMove.Value = ClaimedPlanetMultiplier(gm, curMove.Value);
                    break;
                }
            }

            return curMove;
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

                move.Value = UnclaimedPlanetMultiplier(move.Value) 
                    * (1 + unClaimedPlanet.GetDockingSpots() * _unclaimedPlanetSizeBonus); //Should give a bump to larger planets
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