namespace EAappEmulater.Core;

public class CRandom
{
    private uint _randSeed;

    public void Seed(uint seed)
    {
        _randSeed = seed;
    }

    public int Rand()
    {
        _randSeed = _randSeed * 214013u + 2531011u;
        return (int)(_randSeed >> 16 & 65535u);
    }
}
