using System;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace Halite2
{
    public class MyBot
    {

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

                int shipCount = 0;
                Ship prevShip = null;
                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    shipCount++;
                    if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
                    {
                        prevShip = ship;
                        continue;
                    }

                    var bestMove = GetBestMoveToUnclaimedPlanet(gameMaster, ship, prevShip);
                    var nextMove = GetBestMoveToClaimedPlanet(gameMaster, ship, prevShip, shipCount);
                    EvaluateBestMethod(nextMove, ref bestMove);

                    if (bestMove != null)
                    {
                        moveList.Add(bestMove.Move);
                    }
                    prevShip = ship;
                }

                Networking.SendMoves(moveList);
            }
        }
        
        private static SmartMove GetBestMoveToUnclaimedPlanet(GameMaster gameMaster, Ship ship, Ship prevShip)
        {
            SmartMove bestMove = null;
            foreach (var planet in gameMaster.UnClaimedPlanets)
            {
                if (ship.CanDock(planet))
                {
                    bestMove = new SmartMove(GetDockValue(ship, planet), new DockMove(ship, planet));
                    break;
                }

                var move = NavigateToTarget(ship, prevShip, gameMaster.GameMap, ship.GetClosestPoint(planet));
                if (move == null)
                {
                    continue;
                }

                move.Value = UnclaimedPlanetMultiplier(move.Value);
                EvaluateBestMethod(move, ref bestMove);
            }
            return bestMove;
        }
        
        private static SmartMove GetBestMoveToClaimedPlanet(GameMaster gameMaster, Ship ship, Ship prevShip, int curCount)
        {
            SmartMove bestMove = null;
            SmartMove curMove = null;
            foreach (var planet in gameMaster.ClaimedPlanets)
            {
                //When planet is mine
                if (planet.GetOwner() == gameMaster.MyPlayerId)
                {
                    //curMove = GetBestMoveWhenPlanetIsMine(gameMaster, ship, prevShip, planet);
                    //EvaluateBestMethod(curMove, ref bestMove);
                    continue;
                }

                //When planet is claimed by enemy
                foreach (var shipId in planet.GetDockedShips())
                {
                    //TODO: Verify this works
                    var enemyShip = gameMaster.GameMap.GetShip(planet.GetOwner(), shipId);
                    curMove = NavigateToTarget(ship, prevShip, gameMaster.GameMap, ship.GetClosestPoint(enemyShip));

                    if (curMove == null)
                    {
                        continue;
                    }

                    curMove.Value = ClaimedPlanetMultiplier(curMove.Value, curCount);
                    EvaluateBestMethod(curMove, ref bestMove);
                }
            }
            return bestMove;
        }

        private static SmartMove GetBestMoveWhenPlanetIsMine(GameMaster gameMaster, Ship ship, Ship prevShip, Planet planet)
        {
            if (planet.IsFull()) { return null; }

            if (ship.CanDock(planet))
            {
                return new SmartMove(GetDockValue(ship, planet), new DockMove(ship, planet));
            }
            /*
            var curMove = NavigateToTarget(ship, prevShip, gameMaster.GameMap, ship.GetClosestPoint(planet));

            if (curMove == null)
            {
                return null;
            }
            curMove.Value = MyPlanetMultiplier(curMove.Value);
            return curMove;
            */
            return null;
        }

        /*
         ********* Utilities
         */

        private static void EvaluateBestMethod(SmartMove nextMove, ref SmartMove bestMove)
        {
            if (nextMove != null && nextMove.CompareTo(bestMove) > 0)
            {
                bestMove = nextMove;
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


        /*
         * Navigation
         */


        private static SmartMove NavigateToTarget(Ship ship, Ship prevShip, GameMap gameMap, Position targetPos)
        {
            return NavigateToTarget(ship, prevShip, gameMap, targetPos, _thrust, _maxCorrections, _angularStepRad);
        }

        private static SmartMove NavigateToTarget(
            Ship ship,
            Ship prevShip,
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

            double distance = ship.GetDistanceTo(targetPos);
            double angleRad = ship.OrientTowardsInRad(targetPos);

            //Avoid crashing into a planet and my ships
            if (gameMap.ObjectsBetween(ship, targetPos).Any())
//                .Any(x =>
//                x.GetType() == typeof(Planet) || x.GetOwner() == GameMaster.Instance.MyPlayerId))
            {
                double newTargetDx = Math.Cos(angleRad + angularStepRad) * distance;
                double newTargetDy = Math.Sin(angleRad + angularStepRad) * distance;
                Position newTarget = new Position(ship.GetXPos() + newTargetDx, ship.GetYPos() + newTargetDy);

                return NavigateToTarget(ship, prevShip, gameMap, newTarget, maxThrust, (maxCorrections - 1), angularStepRad);
            }
            else //Determine value of crashing into enemy
            {
                //TODO: Lets ignore for now and find out what happens
            }

            bool prevShipTooClose = prevShip != null && ship.GetDistanceTo(prevShip) < 1;

            int thrust = distance < maxThrust ? (int)distance : maxThrust;
            thrust -= prevShipTooClose ? 1 : 0;
            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            //Increase pt value as you get closer to target
            double ptVal = distance > 0 ? _distanceNumerator / distance : 1 * 100;

            return new SmartMove(ptVal, new ThrustMove(ship, angleDeg, thrust));
        }

        private static void SetupGame(string[] args)
        {
            Networking networking = new Networking();
            GameMap gameMap = networking.Initialize("Meucci");
            GameMaster.Initialize(gameMap);
        }
    }
}
