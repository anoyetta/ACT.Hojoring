using System.Linq;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
    public partial class CombatantEx
    {
        private static readonly short[] TankStanceEffectIDs = new short[]
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

            var si = SharlayanHelper.Instance.CurrentPlayer.StatusItems;
            if (si == null)
            {
                return false;
            }

            return si.Any(x =>
                TankStanceEffectIDs.Contains(x?.StatusID ?? 0));
        }
    }
}
