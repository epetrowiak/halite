using System;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public abstract class AbstractShip : ISmartShip
    {
        public Ship Me { get; }
        public SmartMove BestMove { get; set; }

        protected static readonly double _angularStepRad = Math.PI / 180.0;
        protected static readonly int _thrust = Constants.MAX_SPEED;
        protected static readonly int _maxCorrections = 10;//Constants.MAX_NAVIGATION_CORRECTIONS;

//        protected static readonly double _shipAttackBonus = 12.0;
//        protected static readonly int _shipCountToAttackBonus = 3;
//        protected static readonly double _kamikazeMinPercentage = 0.3;

        protected static readonly double _distanceNumerator = 20.0;
        protected static readonly double _unclaimedPlanetBonus = 4.0;
        protected static readonly double _myPlanetBonus = 0.2;
        protected static readonly double _enemyPlanetBonus = 3.5;
        protected static readonly double _defendPlanetBonus = 2.0;


        protected AbstractShip(Ship ship)
        {
            Me = ship;
        }

        public abstract Move DoWork();


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

            var obstruction = gameMap.ObjectsBetween(Me, targetPos).FirstOrDefault();
            if (obstruction != null)
            {
                var bestTarget = AvoidObstruction(targetPos, obstruction, angleRad, distance);
                return NavigateToTarget(gameMap, bestTarget, maxThrust, (maxCorrections - 1));
            }

            int thrust = distance < maxThrust ? (int)distance : maxThrust;
            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            //Increase pt value as you get closer to target
            double ptVal = distance > 0 ? _distanceNumerator / distance : _distanceNumerator * 100;

            return new SmartMove(ptVal, new ThrustMove(Me, angleDeg, thrust));
        }

        protected Position AvoidObstruction(Position targetPos, Entity obstruction, double angleRad, double distance)
        {
            var distToObstCenter = Me.GetDistanceTo(obstruction);
            var hypotenuse = Math.Sqrt(Math.Pow(obstruction.GetRadius(), 2) + Math.Pow(distToObstCenter, 2));
            var angularStepRad2 = Math.Asin(obstruction.GetRadius() / hypotenuse);

            double newTargetDx = Math.Cos(angleRad + angularStepRad2) * distance;
            double newTargetDy = Math.Sin(angleRad + angularStepRad2) * distance;
            Position newTarget = new Position(Me.GetXPos() + newTargetDx, Me.GetYPos() + newTargetDy);
            double otherTargetDx = Math.Cos(angleRad - angularStepRad2) * distance;
            double otherTargetDy = Math.Sin(angleRad - angularStepRad2) * distance;
            Position otherTarget = new Position(Me.GetXPos() + otherTargetDx, Me.GetYPos() + otherTargetDy);
            var bestTarget = newTarget.GetDistanceTo(targetPos) <= otherTarget.GetDistanceTo(targetPos)
                ? newTarget
                : otherTarget;
            return bestTarget;
        }

        #region Helpers
        protected bool EvaluateBestMethod(SmartMove nextMove)
        {
            if (nextMove != null && nextMove.CompareTo(BestMove) > 0)
            {
                BestMove = nextMove;
                return true;
            }
            return false;
        }

        protected static double GetDockValue(Ship ship, Planet planet)
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

        protected static double DefendPlanetValue(double distVal)
        {
            return _defendPlanetBonus * distVal;
        }


        protected static double UnclaimedPlanetMultiplier(double curValue)
        {
            return _unclaimedPlanetBonus * curValue;
        }

        protected static double MyPlanetMultiplier(double curValue)
        {
            return _myPlanetBonus * curValue;
        }

        protected static double ClaimedPlanetMultiplier(double curValue)
        {
            //Every nth ship, a bonus will be given to attack
            //            var atkBonus = curCount % _shipCountToAttackBonus == 0 ? _shipAttackBonus : 1;
            var atkBonus = 1;

            return _enemyPlanetBonus * curValue * atkBonus;
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