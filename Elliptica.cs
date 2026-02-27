using System.Numerics;

namespace Elliptica;

/// <summary>
/// Represents a point on an elliptic curve over a prime field.
/// </summary>
public readonly struct Point
{
    public BigInteger X { get; }
    public BigInteger Y { get; }
    public bool IsInfinity { get; }

    public static readonly Point Infinity = new(true);

    private Point(bool infinity) => IsInfinity = infinity;

    public Point(BigInteger x, BigInteger y)
    {
        X = x;
        Y = y;
        IsInfinity = false;
    }

    public override string ToString() => IsInfinity ? "O" : $"({X}, {Y})";
}

/// <summary>
/// Elliptic curve in Weierstrass form: y² = x³ + ax + b (mod p)
/// </summary>
public class EllipticCurve
{
    public BigInteger A { get; }
    public BigInteger B { get; }
    public BigInteger P { get; }

    public EllipticCurve(BigInteger a, BigInteger b, BigInteger p)
    {
        A = a;
        B = b;
        P = p;
    }

    /// <summary>
    /// Point addition: P + Q
    /// </summary>
    public Point Add(Point p, Point q)
    {
        if (p.IsInfinity) return q;
        if (q.IsInfinity) return p;
        if (p.X == q.X && p.Y != q.Y) return Point.Infinity;
        if (p.X == q.X && p.Y == q.Y) return Double(p);

        // Slope λ = (y₂ - y₁) / (x₂ - x₁) mod p
        BigInteger dx = (q.X - p.X) % P;
        dx = dx < 0 ? dx + P : dx;
        BigInteger dy = (q.Y - p.Y) % P;
        dy = dy < 0 ? dy + P : dy;

        BigInteger lambda = ModInverse(dx, P) * dy % P;

        BigInteger x = (lambda * lambda - p.X - q.X) % P;
        x = x < 0 ? x + P : x;

        BigInteger y = (lambda * (p.X - x) - p.Y) % P;
        y = y < 0 ? y + P : y;

        return new Point(x, y);
    }

    /// <summary>
    /// Point doubling: 2P
    /// </summary>
    public Point Double(Point p)
    {
        if (p.IsInfinity) return p;

        // Slope λ = (3x₁² + a) / (2y₁) mod p
        BigInteger numerator = (3 * p.X * p.X + A) % P;
        BigInteger denominator = (2 * p.Y) % P;

        if (denominator == 0) return Point.Infinity;

        BigInteger lambda = numerator * ModInverse(denominator, P) % P;

        BigInteger x = (lambda * lambda - 2 * p.X) % P;
        x = x < 0 ? x + P : x;

        BigInteger y = (lambda * (p.X - x) - p.Y) % P;
        y = y < 0 ? y + P : y;

        return new Point(x, y);
    }

    /// <summary>
    /// Scalar multiplication: k * P using double-and-add
    /// </summary>
    public Point Multiply(BigInteger k, Point p)
    {
        if (k == 0) return Point.Infinity;
        if (k < 0) throw new ArgumentException("k must be non-negative");

        var result = Point.Infinity;
        var addend = p;

        while (k > 0)
        {
            if ((k & 1) == 1)
                result = Add(result, addend);
            addend = Double(addend);
            k >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Check if point lies on curve
    /// </summary>
    public bool Contains(Point p)
    {
        if (p.IsInfinity) return true;
        return (p.Y * p.Y) % P == (p.X * p.X * p.X + A * p.X + B) % P;
    }

    private static BigInteger ModInverse(BigInteger a, BigInteger m)
    {
        BigInteger old_r = a, r = m;
        BigInteger old_s = 1, s = 0;

        while (r != 0)
        {
            BigInteger q = old_r / r;
            (old_r, r) = (r, old_r - q * r);
            (old_s, s) = (s, old_s - q * s);
        }

        if (old_r != 1) throw new InvalidOperationException("No inverse exists");
        return old_s < 0 ? old_s + m : old_s;
    }
}
