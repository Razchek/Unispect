namespace Unispect
{
    public class ModuleProxy
    {
        public string Name { get; }
        public ulong BaseAddress { get; }
        public int Size { get; } 

        public ModuleProxy(string name, ulong baseAddress, int size)
        {
            Name = name;
            BaseAddress = baseAddress;
            Size = size;
        }

    }
}