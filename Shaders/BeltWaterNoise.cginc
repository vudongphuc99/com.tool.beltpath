// Procedural noise for the belt water shaders.
// Lattice rows wrap every `periodY` cells, so a pattern sampled with v = 0..N (N an integer) repeats
// exactly at v = 1, 2, ... and a closed belt has no visible seam where it meets itself.
#ifndef BELT_WATER_NOISE_INCLUDED
#define BELT_WATER_NOISE_INCLUDED

// Hash without sin (frac, dot and multiplies only): cheaper on mobile GPUs and stable for large cell values.
float BeltHash(float2 cell)
{
    float3 p3 = frac(cell.xyx * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 BeltHash2(float2 cell)
{
    float3 p3 = frac(cell.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// Wraps a lattice row into 0..periodY-1, also for negative rows.
float BeltWrapRow(float row, float periodY)
{
    return row - periodY * floor(row / periodY);
}

// Smooth value noise in 0..1. p is in lattice cells; periodY is a whole number of cells.
float BeltPeriodicNoise(float2 p, float periodY)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    float2 blend = local * local * (3.0 - 2.0 * local);
    float row0 = BeltWrapRow(cell.y, periodY);
    float row1 = BeltWrapRow(cell.y + 1.0, periodY);

    float a = BeltHash(float2(cell.x, row0));
    float b = BeltHash(float2(cell.x + 1.0, row0));
    float c = BeltHash(float2(cell.x, row1));
    float d = BeltHash(float2(cell.x + 1.0, row1));
    return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
}

// Distance gap F2 - F1 of a cellular pattern: near 0 on cell borders, so 1 - smoothstep gives a bright web.
float BeltPeriodicWeb(float2 p, float periodY)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    // Squared distances keep the same order as distances, so the square root is taken only twice at the end.
    float nearest = 64.0;
    float second = 64.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y);
            float2 id = float2(cell.x + x, BeltWrapRow(cell.y + y, periodY));
            float2 delta = offset + BeltHash2(id) - local;
            float distSq = dot(delta, delta);
            second = min(second, max(distSq, nearest));
            nearest = min(nearest, distSq);
        }
    }

    return sqrt(second) - sqrt(nearest);
}

#endif
