using System.Linq;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
    public partial class CombatantEx
    {
        private static readonly uint[] TankStanceEffectIDs = new uint[]
        {
            91,     // ディフェンダー
            1833,   // ロイヤルガード
            79,     // アイアンウィル
            743,    // グリットスタンス
        };

        public bool InTankStance()
        {
            if (this.ActorType != Actor.Type.PC ||
                this.Role != Roles.Tank)
            {
                return false;
            }

            if (this.Effects == null)
            {
                return false;
            }

            return this.Effects.Any(x =>
                TankStanceEffectIDs.Contains(x?.BuffID ?? 0));
        }
    }
}
