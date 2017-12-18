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
        public int? DockingPlanetId { get; set; }

        public bool IsDockingPlanetFull(GameMap gameMap)
        {
            //If No value, then not docking so planet is full
            return !DockingPlanetId.HasValue || gameMap.GetPlanet(DockingPlanetId.Value).IsFull();
        } 

        private static readonly double _angularStepRad = Math.PI / 180.0;
        private static readonly int _thrust = Constants.MAX_SPEED;
        private static readonly int _maxCorrections = 8;//Constants.MAX_NAVIGATION_CORRECTIONS;

        private static readonly double _distanceNumerator = 20.0;
        private static readonly double _shipAttackBonus = 4.0;
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
            var myDockingStatus = Me.GetDockingStatus();
            if (myDockingStatus != Ship.DockingStatus.Undocked)
            {
                return null;
            }
            //            if (myDockingStatus == Ship.DockingStatus.Docked)
            //            {
            //                return null;
            //            }
            //            if (myDockingStatus == Ship.DockingStatus.Docking)
            //            {
            //                if (IsDockingPlanetFull(gm.GameMap))
            //                {
            //                   DockingPlanetId = null;
            //                }
            //                else
            //                {
            //                    return null;
            //                }
            //            }

            //Check Unclaimed Planet move
            BestMoveToUnclaimedPlanet(gm);

            //Check Claimed Planet move
            BestMoveToClaimedPlanet(gm);

            //Check Move based on game state
            //            nextMove = BestGameMove(gm);
//            EvaluateBestMove(nextMove);

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

            SmartMove myMove = null;
            int numDocked = claimedPlanet.GetDockedShips().Count;
            int remainingSpots = claimedPlanet.GetDockingSpots() - numDocked;

            if (Me.CanDock(claimedPlanet))
            {
                myMove = new SmartMove(
                    GetDockValue(Me, claimedPlanet) * ((double)remainingSpots / 2)
                    , new DockMove(Me, claimedPlanet), claimedPlanet.GetId());
//                var closestPoint = Me.GetClosestPoint(claimedPlanet);
//                foreach (var shipId in claimedPlanet.GetDockedShips())
//                {
//                    
//                }
            }
            else
            {
//                myMove = NavigateToTarget(gm.GameMap, claimedPlanet);
            }

            EvaluateBestMove(myMove);
        }

        private void ClaimEnemyPlanetMove(GameMaster gm, Planet claimedPlanet)
        {
            SmartMove curMove;
            foreach (var shipId in claimedPlanet.GetDockedShips())
            {
                var enemyShip = gm.GameMap.GetShip(claimedPlanet.GetOwner(), shipId);
                curMove = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(enemyShip));

                if (curMove == null)
                {
                    continue;
                }

                curMove.Value = ClaimedPlanetMultiplier(curMove.Value, gm.PreviousShips.Count);
                EvaluateBestMove(curMove);
            }
        }

        private void BestMoveToUnclaimedPlanet(GameMaster gm)
        {
            foreach (var unClaimedPlanet in gm.UnClaimedPlanets)
            {
                if (Me.CanDock(unClaimedPlanet))
                {
                    var curMove = new SmartMove(GetDockValue(Me, unClaimedPlanet), new DockMove(Me, unClaimedPlanet), unClaimedPlanet.GetId());
                    EvaluateBestMove(curMove);
                    break;
                }

                var move = NavigateToTarget(gm.GameMap, Me.GetClosestPoint(unClaimedPlanet));
                EvaluateBestMove(move);
            }

        }

        private SmartMove NavigateToTarget(GameMap gameMap, Position targetPos)
        {
            return NavigateToTarget(gameMap, targetPos, _thrust, _maxCorrections);
        }
        
        private SmartMove NavigateToTarget(
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

            var obstruction = gameMap.ObjectsBetween(Me, targetPos).FirstOrDefault(
                x => x.GetType() == typeof(Planet) ||
                    (x.GetType() == typeof(Ship) && x.GetOwner() == Me.GetId())); //Ignore enemy ships
            if (obstruction != null)
            {
                var bestTarget = AvoidObstruction(targetPos, obstruction, angleRad, distance);
                return NavigateToTarget(gameMap, bestTarget, maxThrust, (maxCorrections - 1));
            }
            
            int thrust = distance < maxThrust ? (int) distance : maxThrust;
            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            //Increase pt value as you get closer to target
            double ptVal = distance > 0 ? _distanceNumerator / distance : _distanceNumerator * 100;

            return new SmartMove(ptVal, new ThrustMove(Me, angleDeg, thrust));
        }

        private Position AvoidObstruction(Position targetPos, Entity obstruction, double angleRad, double distance)
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
        private void EvaluateBestMove(SmartMove nextMove)
        {
            //Next move is not greater than current best move
            if (nextMove == null || nextMove.CompareTo(BestMove) <= 0)
            {
                return;
            }

            BestMove = nextMove;
            if (BestMove.IsDockingMove)
            {
                DockingPlanetId = BestMove.DockingToPlanetId;
            }
            else
            {
                DockingPlanetId = null;
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

            return UnclaimedPlanetMultiplier(distVal);
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
        public bool IsDockingMove { get; set; }
        public int DockingToPlanetId { get; set; }
        public Move Move { get; set; }

        public SmartMove(double value, Move move)
        {
            Value = value;
            Move = move;
            IsDockingMove = false;
        }

        public SmartMove(double value, DockMove move, int planetId)
        {
            Value = value;
            Move = move;
            IsDockingMove = true;
            DockingToPlanetId = planetId;
        }

        public int CompareTo(SmartMove other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Value.CompareTo(other.Value);
        }
    }
}