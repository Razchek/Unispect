using System;

namespace Unispect
{
    [Serializable]
    public struct MonoType // MonoType
    {
        public ulong Data;
        public int Attributes;
        public int Modifiers;

        public TypeEnum GetTypeCode()
        {
            return (TypeEnum)(0xFF & (Attributes >> 16));
        }
        /*
        unsigned int attrs    : 16; // param attributes or field flags 
        MonoTypeEnum type     : 8;
        unsigned int num_mods : 6;  // max 64 modifiers follow at the end 
        unsigned int byref    : 1;
        unsigned int pinned   : 1;  // valid when included in a local var signature 
        MonoCustomMod modifiers[MONO_ZERO_LEN_ARRAY]; // this may grow
        */
    }
}