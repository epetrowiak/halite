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

        private static readonly double _distanceNumerator = 10.0;
        private static readonly double _shipAttackBonus = 2.0;
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
                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
                    {
                        continue;
                    }

                    var bestMove = GetBestMoveToUnclaimedPlanet(gameMaster, ship);
                    var nextMove = GetBestMoveToClaimedPlanet(gameMaster, ship, shipCount++);

                    if (nextMove != null && nextMove.CompareTo(bestMove) > 0)
                    {
                        bestMove = nextMove;
                    }

                    if (bestMove != null)
                    {
                        moveList.Add(bestMove.Move);
                    }
                }

                Networking.SendMoves(moveList);
            }
        }

        private static SmartMove GetBestMoveToUnclaimedPlanet(GameMaster gameMaster, Ship ship)
        {
            SmartMove bestMove = null;
            foreach (Planet planet in gameMaster.UnClaimedPlanets)
            {
                if (ship.CanDock(planet))
                {
                    bestMove = new SmartMove(double.MaxValue, new DockMove(ship, planet));
                    break;
                }

                var move = NavigateToTarget(ship, gameMaster.GameMap, ship.GetClosestPoint(planet));
                if (move == null)
                {
                    continue;
                }

                move.Value = UnclaimedPlanetMultiplier(move.Value);
                if (move.CompareTo(bestMove) > 0)
                {
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private static SmartMove GetBestMoveToClaimedPlanet(GameMaster gameMaster, Ship ship, int curCount)
        {
            SmartMove bestMove = null;
            foreach (Planet planet in gameMaster.ClaimedPlanets)
            {
                //Do nothing to my planet
                if (planet.GetOwner() == gameMaster.MyPlayerId) { continue; }

                foreach (var shipId in planet.GetDockedShips())
                {
                    //TODO: Verify this works
                    var enemyShip = gameMaster.GameMap.GetShip(planet.GetOwner(), shipId);
                    var move = NavigateToTarget(ship, gameMaster.GameMap, ship.GetClosestPoint(enemyShip));

                    if (move == null)
                    {
                        continue;
                    }

                    move.Value = ClaimedPlanetMultiplier(move.Value, curCount);
                    if (move.CompareTo(bestMove) > 0)
                    {
                        bestMove = move;
                    }
                }
            }
            return bestMove;
        }

        private static double UnclaimedPlanetMultiplier(double curValue)
        {
            return 2.0 * curValue;
        }

        private static double ClaimedPlanetMultiplier(double curValue, int curCount)
        {
            //Every nth ship, a bonus will be given to attack
            var atkBonus = curCount % _shipCountToAttackBonus == 0 ? _shipAttackBonus : 1;

            return 1.0 * curValue * atkBonus;
        }

        private static SmartMove NavigateToTarget(Ship ship, GameMap gameMap, Position targetPos)
        {
            return NavigateToTarget(ship, gameMap, targetPos, _thrust, _maxCorrections, _angularStepRad);
        }

        private static SmartMove NavigateToTarget(
            Ship ship,
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

                return NavigateToTarget(ship, gameMap, newTarget, maxThrust, (maxCorrections - 1), angularStepRad);
            }
            else //Determine value of crashing into enemy
            {
                //TODO: Lets ignore for now and find out what happens
            }

            int thrust = distance < maxThrust ? (int)distance : maxThrust;
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
