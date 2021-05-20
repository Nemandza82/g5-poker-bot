#pragma once

class ParkMillerCarta
{
    int seed;

public:

    ParkMillerCarta(int aSeed)
    {
        seed = aSeed;
    }

    unsigned int next()
    {
        unsigned int lo = 16807 * (seed & 0xFFFF);
        unsigned int hi = 16807 * (seed >> 16);
        
        lo += (hi & 0x7FFF) << 16;
        lo += (hi >> 15);

        if (lo > 0x7FFFFFFF)
            lo -= 0x7FFFFFFF;

        seed = (int) lo;
        return seed;
    }

};
