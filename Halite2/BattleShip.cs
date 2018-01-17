using System;
using Halite2.hlt;

namespace Halite2
{
    public class BattleShip : AbstractShip
    {
        public BattleShip(Ship ship) : base(ship)
        {
        }

        public override Move DoWork()
        {
            var gm = GameMaster.Instance;
            if (Me.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return null;
            }

            var myMove = DoBattleWithNearestEnemy(gm);

            if (myMove != null)
            {
                Target = myMove.Target;
            }

            return myMove?.Move;
        }

        public override Move ReactToMyShip(ISmartShip otherShip)
        {
            return DoWork();
        }

        private SmartMove DoBattleWithNearestEnemy(GameMaster gameMaster)
        {
            SmartMove bestMove = null;
            foreach (var enemyShip in gameMaster.EnemyShips)
            {
                var smartMove = NavigateToTarget(gameMaster.GameMap, Me.GetClosestPoint(enemyShip), _thrust, _maxCorrections * 2);

                if (smartMove == null)
                {
                    continue;
                }

                bestMove = EvaluateBestMethod(smartMove, bestMove);
            }

            return bestMove;
        }
    }
}