﻿using Halite2.hlt;
using System.Collections.Generic;

namespace Halite2
{
    public class MyBot
    {
        public static void Main(string[] args)
        {
            SetupGame(args);

            while (true)
            {
                var moveList = GameMaster.Instance.PlayTurn(Networking.ReadLineIntoMetadata());

                Networking.SendMoves(moveList);

                //Remove..
                foreach (Ship ship in gameAwareness.GameMap.GetMyPlayer().GetShips().Values)
                {
                    if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
                    {
                        continue;
                    }

                    foreach (Planet planet in gameAwareness.GetAllPlanets().Values)
                    {
                        if (planet.IsOwned())
                        {
                            continue;
                        }

                        if (ship.CanDock(planet))
                        {
                            moveList.Add(new DockMove(ship, planet));
                            break;
                        }

                        ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameAwareness, ship, planet, Constants.MAX_SPEED / 2);
                        if (newThrustMove != null)
                        {
                            moveList.Add(newThrustMove);
                        }

                        break;
                    }
                }
            }
        }

//        public static void Main(string[] args)
//        {
//            var gameMap = SetupGame(args);
//
//            List<Move> moveList = new List<Move>();
//            while(true)
//            {
//                moveList.Clear();
//                gameMap.UpdateMap(Networking.ReadLineIntoMetadata());
//
//                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
//                {
//                    if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
//                    {
//                        continue;
//                    }
//
//                    foreach (Planet planet in gameMap.GetAllPlanets().Values)
//                    {
//                        if (planet.IsOwned())
//                        {
//                            continue;
//                        }
//
//                        if (ship.CanDock(planet))
//                        {
//                            moveList.Add(new DockMove(ship, planet));
//                            break;
//                        }
//
//                        ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED / 2);
//                        if (newThrustMove != null)
//                        {
//                            moveList.Add(newThrustMove);
//                        }
//
//                        break;
//                    }
//                }
//                Networking.SendMoves(moveList);
//            }
//        }

        private static void SetupGame(string[] args)
        {
            string name = args.Length > 0 ? args[0] : "Sharpie";

            Networking networking = new Networking();
            GameMap gameMap = networking.Initialize(name);
            GameMaster.Instance.GameMap = gameMap;
        }
    }
}
