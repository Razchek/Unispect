using System;

namespace Unispect
{
    [Serializable]
    public struct MonoType // MonoType
    {
        public ulong Data;
        public int Attributes;
        public int Modifiers;

        public bool IsStatic => (Attributes & 0x10) == 0x10;

        public bool IsConstant => (Attributes & 0x40) == 0x40;

        public bool HasValue => IsConstant || IsStatic;

        public TypeEnum TypeCode => (TypeEnum)(0xFF & (Attributes >> 16));
    }
}

//struct _MonoType
//{
//    union {
//        MonoClass* klass; /* for VALUETYPE and CLASS */
//        MonoType* type;   /* for PTR */
//        MonoArrayType* array; /* for ARRAY */
//        MonoMethodSignature* method;
//        MonoGenericParam* generic_param; /* for VAR and MVAR */
//        MonoGenericClass* generic_class; /* for GENERICINST */
//    }
//    data;
//    unsigned int attrs    : 16; /* param attributes or field flags */
//    MonoTypeEnum type     : 8;
//    unsigned int num_mods : 6;  /* max 64 modifiers follow at the end */
//    unsigned int byref    : 1;
//    unsigned int pinned   : 1;  /* valid when included in a local var signature */
//    MonoCustomMod modifiers[MONO_ZERO_LEN_ARRAY]; /* this may grow */
//};