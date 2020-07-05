namespace Unispect
{
    /// <summary>
    /// Represents the type of an object in managed memory.
    /// See: MonoTypeEnum in https://github.com/Unity-Technologies/mono/blob/unity-master/mono/metadata/blob.h.
    /// </summary>
    public enum TypeEnum // MonoTypeEnum
    {
        End = 0x00, // End of list

        Void = 0x01,

        Boolean = 0x02,
        Char = 0x03,
        Byte = 0x04,
        SByte = 0x05,

        Int16 = 0x06,
        UInt16 = 0x07,

        Int32 = 0x08,
        UInt32 = 0x09,

        Int64 = 0x0A,
        UInt64 = 0x0B,

        Single = 0x0C,
        Double = 0x0d,

        String = 0x0e,

        Ptr = 0x0f,         /* arg: <type> token */
        ByRef = 0x10,       /* arg: <type> token */
        ValueType = 0x11,   /* arg: <type> token */
        Class = 0x12,       /* arg: <type> token */
        Var = 0x13,         /* number */
        Array = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
        GenericInst = 0x15, /* <type> <type-arg-count> <type-1> \x{2026} <type-n> */
        TypedByRef = 0x16,

        IntPtr = 0x18,
        UIntPtr = 0x19,

        FnPtr = 0x1b,       /* arg: full method signature */

        Object = 0x1c,

        SzArray = 0x1d,     /* 0-based one-dim-array */
        MVar = 0x1e,        /* number */

        CModReqd = 0x1f,   /* arg: typedef or typeref token */
        CModOpt = 0x20,    /* optional arg: typedef or typref token */
        Internal = 0x21,    /* CLR internal type */
        Modifier = 0x40,    /* Or with the following types */
        Sentinel = 0x41,    /* Sentinel for varargs method signature */
        Pinned = 0x45,      /* Local var that points to pinned object */
        Enum = 0x55         /* an enumeration */

        /* Taken from IDA (I had issues, seems they were all valid anyway ..)
             ; enum MonoTypeEnum,
             MONO_TYPE_END    = 0
             MONO_TYPE_VOID   = 1
             MONO_TYPE_BOOLEAN  = 2
             MONO_TYPE_CHAR   = 3
             MONO_TYPE_I1     = 4
             MONO_TYPE_U1     = 5
             MONO_TYPE_I2     = 6
             MONO_TYPE_U2     = 7
             MONO_TYPE_I4     = 8
             MONO_TYPE_U4     = 9
             MONO_TYPE_I8     = 0Ah
             MONO_TYPE_U8     = 0Bh
             MONO_TYPE_R4     = 0Ch
             MONO_TYPE_R8     = 0Dh
             MONO_TYPE_STRING  = 0Eh
             MONO_TYPE_PTR    = 0Fh
             MONO_TYPE_BYREF  = 10h
             MONO_TYPE_VALUETYPE  = 11h
             MONO_TYPE_CLASS  = 12h
             MONO_TYPE_VAR    = 13h
             MONO_TYPE_ARRAY  = 14h
             MONO_TYPE_GENERICINST  = 15h
             MONO_TYPE_TYPEDBYREF  = 16h
             MONO_TYPE_I      = 18h
             MONO_TYPE_U      = 19h
             MONO_TYPE_FNPTR  = 1Bh
             MONO_TYPE_OBJECT  = 1Ch
             MONO_TYPE_SZARRAY  = 1Dh
             MONO_TYPE_MVAR   = 1Eh
             MONO_TYPE_CMOD_REQD  = 1Fh
             MONO_TYPE_CMOD_OPT  = 20h
             MONO_TYPE_INTERNAL  = 21h
             MONO_TYPE_MODIFIER  = 40h
             MONO_TYPE_SENTINEL  = 41h
             MONO_TYPE_PINNED  = 45h
             MONO_TYPE_ENUM   = 55h
        */
    }
}