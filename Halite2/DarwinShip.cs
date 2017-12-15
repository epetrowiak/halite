using System;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public class DarwinShip
    {
        public Ship Me { get; }
        public SmartMove BestMove { get; set; }

        private static readonly double _angularStepRad = Math.PI / 180.0;
        private static readonly int _thrust = Constants.MAX_SPEED;
        private static readonly int _maxCorrections = Constants.MAX_NAVIGATION_CORRECTIONS;

        private static readonly double _distanceNumerator = 20.0;
        private static readonly double _shipAttackBonus = 2.0;
        private static readonly double _unclaimedPlanetBonus = 4.0;
        private static readonly double _myPlanetBonus = 1.5;
        private static readonly double _enemyPlanetBonus = 1.0;

        private static readonly int _shipCountToAttackBonus = 4;
        private static readonly double _kamikazeMinPercentage = 0.3;

        public DarwinShip(Ship ship)
        {
            Me = ship;
        }

        public Move DoWork()
        {
            var gm = GameMaster.Instance;
            if (Me.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return null;
            }

            //Check Unclaimed Planet move
            //1st Move is current best move
            BestMove = BestMoveToUnclaimedPlanet(gm);

            //Check Claimed Planet move
            var nextMove = BestMoveToClaimedPlanet(gm);
            //Check next move against best move
            EvaluateBestMethod(nextMove);

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

        private SmartMove BestMoveToClaimedPlanet(GameMaster gm)
        {
            SmartMove bestMove = null;
            SmartMove curMove = null;
            foreach (var claimedPlanet in gm.ClaimedPlanets)
            {
                //Do nothing to my planet
                if(claimedPlanet.GetOwner() == gm.MyPlayerId) { continue; }

                foreach (var shipId in claimedPlanet.GetDockedShips())
                {
                    var enemyShip = gm.GameMap.GetShip(claimedPlanet.GetOwner(), shipId);
                    curMove = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));

                    if (curMove == null)
                    {
                        continue;
                    }

                    curMove.Value = ClaimedPlanetMultiplier(curMove.Value, gm.PreviousShips.Count);
                    EvaluateBestMethod(curMove);
                }

            }
            return bestMove;
        }

        private SmartMove BestMoveToUnclaimedPlanet(GameMaster gm)
        {
            SmartMove bestMove = null;
            foreach (var unClaimedPlanet in gm.UnClaimedPlanets)
            {
                if (Me.CanDock(unClaimedPlanet))
                {
                    bestMove = new SmartMove(GetDockValue(Me, unClaimedPlanet), new DockMove(Me, unClaimedPlanet));
                    break;
                }

                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(unClaimedPlanet));

                if (move!= null && move.CompareTo(bestMove) > 0)
                {
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private SmartMove NavigateToTarget(GameMap gameMap, Position targetPos)
        {
            return NavigateToTarget(gameMap, targetPos, _thrust, _maxCorrections, _angularStepRad);
        }
        
        private SmartMove NavigateToTarget(
                GameMap gameMap,
                Position targetPos,
                int maxThrust,
                int maxCorrections,
                double angularStepRad)
        {
            if (maxCorrections <= 0)
            {
                return null;
            }

            double distance = Me.GetDistanceTo(targetPos);
            double angleRad = Me.OrientTowardsInRad(targetPos);

            //Avoid crashing into a planet and my ships
            if (gameMap.ObjectsBetween(Me, targetPos).Any())
            {
                double newTargetDx = Math.Cos(angleRad + angularStepRad) * distance;
                double newTargetDy = Math.Sin(angleRad + angularStepRad) * distance;
                Position newTarget = new Position(Me.GetXPos() + newTargetDx, Me.GetYPos() + newTargetDy);

                return NavigateToTarget(gameMap, newTarget, maxThrust, (maxCorrections - 1), angularStepRad);
            }
            else //Determine value of crashing into enemy
            {
                //TODO: Lets ignore for now and find out what happens
            }
            
            int thrust = distance < maxThrust ? (int) distance : maxThrust;
            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            //Increase pt value as you get closer to target
            double ptVal = distance > 0 ? _distanceNumerator / distance : _distanceNumerator * 100;

            return new SmartMove(ptVal, new ThrustMove(Me, angleDeg, thrust));
        }

        #region Helpers
        private void EvaluateBestMethod(SmartMove nextMove)
        {
            if (nextMove != null && nextMove.CompareTo(BestMove) > 0)
            {
                BestMove = nextMove;
            }
        }

        private static double GetDockValue(Ship ship, Planet planet)
        {
            //If I own the planet already
            var dist = ship.GetDistanceTo(ship.GetClosestPoint(planet));
            var distVal = _distanceNumerator / dist;
            if (planet.IsOwned() && planet.GetOwner() == ship.GetOwner())
            {
                return MyPlanetMultiplier(distVal);
            }

            return distVal * 2;
        }

        private static double UnclaimedPlanetMultiplier(double curValue)
        {
            return _unclaimedPlanetBonus * curValue;
        }

        private static double MyPlanetMultiplier(double curValue)
        {
            return _myPlanetBonus * curValue;
        }

        private static double ClaimedPlanetMultiplier(double curValue, int curCount)
        {
            //Every nth ship, a bonus will be given to attack
            var atkBonus = curCount % _shipCountToAttackBonus == 0 ? _shipAttackBonus : 1;

            return _enemyPlanetBonus * curValue * atkBonus;
        }


        #endregion

    }

    public class SmartMove : IComparable<SmartMove>
    {
        public double Value { get; set; }
        public Move Move { get; set; }

        public SmartMove(double value, Move move)
        {
            Value = value;
            Move = move;
        }

        public int CompareTo(SmartMove other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Value.CompareTo(other.Value);
        }
    }
}