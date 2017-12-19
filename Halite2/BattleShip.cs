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

            DoBattleWithNearestEnemy(gm);

            return BestMove?.Move;
        }

        private void DoBattleWithNearestEnemy(GameMaster gameMaster)
        {
            foreach (var enemyShip in gameMaster.EnemyShips)
            {
                var smartMove = NavigateToTarget(gameMaster.GameMap, Me.GetClosestPoint(enemyShip), _thrust, _maxCorrections);
                EvaluateBestMethod(smartMove);
            }
        }
    }
}