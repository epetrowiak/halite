using System;
using System.Collections.Generic;
using System.Linq;
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

            //Check Unclaimed Planet move
            BestMoveToUnclaimedPlanet(gm);

            //Check Claimed Planet move
            BestMoveToClaimedPlanet(gm);

            //Check Move based on game state
            //            nextMove = BestGameMove(gm);
//            EvaluateBestMethod(nextMove);


            return BestMove?.Move;
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

        private void BestMoveToClaimedPlanet(GameMaster gm)
        {
            foreach (var claimedPlanet in gm.ClaimedPlanets)
            {
                //Do nothing to my planet
                if (claimedPlanet.GetOwner() == gm.MyPlayerId)
                {
                    DockToMyPlanetMove(gm, claimedPlanet);
                }
                else
                {
                    ClaimEnemyPlanetMove(gm, claimedPlanet);
                }
            }
        }

        private void DockToMyPlanetMove(GameMaster gm, Planet claimedPlanet)
        {
            if (claimedPlanet.IsFull())
            {
                return;
            }

            TryDockToPlanet(claimedPlanet);
        }


        private void ClaimEnemyPlanetMove(GameMaster gm, Planet enemyPlanet)
        {
            SmartMove curMove;
            foreach (var shipId in enemyPlanet.GetDockedShips())
            {
                var enemyShip = gm.GameMap.GetShip(enemyPlanet.GetOwner(), shipId);
                curMove = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));

                if (curMove == null)
                {
                    continue;
                }

                curMove.Value = ClaimedPlanetMultiplier(curMove.Value);
                EvaluateBestMethod(curMove);
            }
        }

        private void BestMoveToUnclaimedPlanet(GameMaster gm)
        {
            foreach (var unClaimedPlanet in gm.UnClaimedPlanets)
            {
                if (TryDockToPlanet(unClaimedPlanet)) break;

                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(unClaimedPlanet));

                if (move == null)
                {
                    continue;
                }

                move.Value = UnclaimedPlanetMultiplier(move.Value);
                EvaluateBestMethod(move);
            }
        }

        private bool TryDockToPlanet(Planet planet)
        {
            if (!Me.CanDock(planet))
            {
                return false;
            }

            var gm = GameMaster.Instance;
            foreach (var enemyShip in gm.EnemyShips)
            {
                if (enemyShip.GetDistanceTo(planet) > 5) //Too far away, dont care
                {
                    continue;
                }

                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));
                if (move == null)
                {
                    continue;
                }

                move.Value = DefendPlanetValue(move.Value);
                EvaluateBestMethod(move);
            }

            var curMove = new SmartMove(GetDockValue(Me, planet), new DockMove(Me, planet));
            return EvaluateBestMethod(curMove);
        }

    }

}