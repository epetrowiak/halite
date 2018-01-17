﻿using System;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public abstract class AbstractShip : ISmartShip
    {
        public Ship Me { get; }
        public Position Target { get; set; }

        protected static readonly double _angularStepRad = Math.PI / 180.0;
        protected static readonly int _thrust = Constants.MAX_SPEED;
        protected static readonly int _maxCorrections = 8;//Constants.MAX_NAVIGATION_CORRECTIONS;

        protected static readonly double _shipAttackBonus = 2.0;
//        protected static readonly int _shipCountToAttackBonus = 3;
//        protected static readonly double _kamikazeMinPercentage = 0.3;

        protected static readonly double _distanceNumerator = 20.0;
        protected static readonly double _unclaimedPlanetBonus = 2.0;
        protected static readonly double _unclaimedPlanetSizeBonus = 0.3;
        protected static readonly double _myPlanetBonus = 0.2;
        protected static readonly double _enemyPlanetBonus = 2.2;
        protected static readonly double _defendPlanetBonus = 1.5;


        protected AbstractShip(Ship ship)
        {
            Me = ship;
        }

        public abstract Move DoWork();
        public abstract Move ReactToMyShip(ISmartShip otherShip);


        protected SmartMove NavigateToTarget(GameMap gameMap, Position targetPos)
        {
            return NavigateToTarget(gameMap, targetPos, _thrust, _maxCorrections);
        }

        protected SmartMove NavigateToTarget(
                GameMap gameMap,
                Position targetPos,
                int maxThrust,
                int maxCorrections)
        {
            if (maxCorrections <= 0)
            {
                return null;
            }

            double distance = Me.GetDistanceTo(targetPos);
            double angleRad = Me.OrientTowardsInRad(targetPos);

            var obstruction = ClosestObstruction(gameMap, targetPos);
            if (obstruction != null)
            {
                var bestTarget = AvoidObstruction(targetPos, obstruction, angleRad, distance);
                return NavigateToTarget(gameMap, bestTarget, maxThrust, (maxCorrections - 1));
            }

            int thrust = distance < maxThrust ? (int)distance : maxThrust;
            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            //Increase pt value as you get closer to target
            double ptVal = distance > 0 ? _distanceNumerator / distance : _distanceNumerator * 100;

            return new SmartMove(ptVal, new ThrustMove(Me, angleDeg, thrust), targetPos);
        }

        private Entity ClosestObstruction(GameMap gameMap, Position targetPos)
        {
            var dist = double.MaxValue;
            Entity closest = null;
            foreach (var obstruction in gameMap.ObjectsBetween(Me, targetPos))
            {
//                if (obstruction.GetOwner() == Me.GetOwner() && obstruction.GetType() == typeof(Ship))
//                {
//                    Ship ship = (Ship) obstruction;
//                    if (ship.GetDockingStatus() == Ship.DockingStatus.Undocked)
//                    {
//                        continue; //My ship that is undocked is not an obstruction
//                    }
//                }

                var curDist = Me.GetDistanceTo(obstruction);
                if (curDist < dist)
                {
                    closest = obstruction;
                }
            }
            return closest;
        }

        protected Position AvoidObstruction(Position targetPos, Entity obstruction, double angleRad, double distance)
        {
            var distToObstCenter = Me.GetDistanceTo(obstruction);
            var hypotenuse = Math.Sqrt(Math.Pow(obstruction.GetRadius(), 2) + Math.Pow(distToObstCenter, 2));
            var angularStepRad2 = Math.Asin(obstruction.GetRadius() / hypotenuse);

            double newTargetDx = Math.Cos(angleRad + angularStepRad2) * distance;
            double newTargetDy = Math.Sin(angleRad + angularStepRad2) * distance;
            Position newTarget = new Position(Me.GetXPos() + newTargetDx, Me.GetYPos() + newTargetDy);
//            double otherTargetDx = Math.Cos(angleRad - angularStepRad2) * distance;
//            double otherTargetDy = Math.Sin(angleRad - angularStepRad2) * distance;
//            Position otherTarget = new Position(Me.GetXPos() + otherTargetDx, Me.GetYPos() + otherTargetDy);
//            var bestTarget = newTarget.GetDistanceTo(targetPos) <= otherTarget.GetDistanceTo(targetPos)
//                ? newTarget
//                : otherTarget;
            return newTarget;
        }


        #region Helpers
        protected SmartMove EvaluateBestMethod(SmartMove nextMove, SmartMove bestMove)
        {
            if (nextMove != null && nextMove.CompareTo(bestMove) > 0)
            {
                return nextMove;
            }
            return bestMove;
        }

        protected double GetDockValue(Ship ship, Planet planet)
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

        protected double DefendPlanetValue(double distVal)
        {
            return _defendPlanetBonus * distVal;
        }


        protected double UnclaimedPlanetMultiplier(double distVal)
        {
            return _unclaimedPlanetBonus * distVal;
        }

        protected double MyPlanetMultiplier(double curValue)
        {
            return _myPlanetBonus * curValue;
        }

        protected double ClaimedPlanetMultiplier(GameMaster gm, double distVal)
        {
            //Every nth ship, a bonus will be given to attack
            var atkBonus = gm.HasSatisfactoryProduction && Me.GetId() % 2 == 1 ? _shipAttackBonus : 1;
//            var atkBonus = 1;

            return _enemyPlanetBonus * distVal * atkBonus;
        }


        #endregion
    }

    public enum ShipType
    {
        Normal,
        Battle
    }

    public class SmartMove : IComparable<SmartMove>
    {
        public double Value { get; set; }
        public Move Move { get; set; }
        public Position Target { get; set; }

        public SmartMove(double value, Move move)
        {
            Value = value;
            Move = move;
        }

        public SmartMove(double value, Move move, Position target)
        {
            Value = value;
            Move = move;
            Target = target;
        }

        public int CompareTo(SmartMove other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Value.CompareTo(other.Value);
        }
    }
}