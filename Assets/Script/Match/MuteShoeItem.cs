using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public class MuteShoeItem : RobberItem
    {
        public override void UseItem(RobberController robber)
        {
            // Local effect
            robber.EnableMuteShoe();
            base.UseItem(robber);
        }
    }
}
