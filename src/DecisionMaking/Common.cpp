#include "Common.h"


namespace G5Cpp
{
    int divUp(int val, int mul)
    {
        return (val >= 0) ? ((val + mul - 1) / mul) : (-divUp(-val, mul));
    }

    int alignUp(int val, int mul)
    {
        return divUp(val, mul) * mul;
    }
}
