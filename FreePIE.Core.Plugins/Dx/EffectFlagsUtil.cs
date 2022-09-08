using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins.Dx
{
    public static class EffectFlagsUtil
    {
        public static void AddTo(this EffectParameterFlags add, ref EffectParameterFlags addTo)
        {
            if (addTo != EffectParameterFlags.All)
                addTo |= add;
        }

        public static void RemoveFrom(this EffectParameterFlags add, ref EffectParameterFlags addTo)
        {
            addTo &= ~add;
        }
        public static void Toggle(this EffectParameterFlags add, ref EffectParameterFlags addTo)
        {
            addTo ^= add;
        }

    }
}
