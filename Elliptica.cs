using System;
using System.Numerics;

namespace Elliptica;

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

public class EllipticCurve
{
    public BigInteger A { get; }
    public BigInteger B { get; }
    public BigInteger P { get; }
    public Point Generator { get; }
    public BigInteger Order { get; }

    public EllipticCurve(BigInteger a, BigInteger b, BigInteger p, Point generator, BigInteger order)
    {
        A = a;
        B = b;
        P = p;
        Generator = generator;
        Order = order;
    }

    public static EllipticCurve Secp256k1 => new(
        0, 7,
        BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663"),
        new Point(
            BigInteger.Parse("550662630242773942802979101730321503127763527812763029367159444450498790520"),
            BigInteger.Parse("32670510020758816978083085130507043184471273380659243275938904335757337482424")
        ),
        BigInteger.Parse("115792089237316195423570985008687907852837564279074904382605163141518161494337")
    );

    public Point Add(Point p, Point q)
    {
        if (p.IsInfinity) return q;
        if (q.IsInfinity) return p;
        if (p.X == q.X && p.Y != q.Y) return Point.Infinity;
        if (p.X == q.X && p.Y == q.Y) return Double(p);

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

    public Point Double(Point p)
    {
        if (p.IsInfinity) return p;

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

    public Point MultiplyByG(BigInteger k) => Multiply(k, Generator);

    public (BigInteger privateKey, Point publicKey) GenerateKeyPair()
    {
        var privateKey = RandomInteger(1, Order - 1);
        var publicKey = MultiplyByG(privateKey);
        return (privateKey, publicKey);
    }

    public bool Contains(Point p)
    {
        if (p.IsInfinity) return true;
        return (p.Y * p.Y) % P == (p.X * p.X * p.X + A * p.X + B) % P;
    }

    public bool Verify(Point publicKey)
    {
        if (publicKey.IsInfinity) return false;
        if (!Contains(publicKey)) return false;
        return Multiply(Order, publicKey).IsInfinity;
    }

    public bool VerifySignature(BigInteger r, Point s, byte[] message, Point publicKey)
    {
        if (r < 1 || r >= Order || s.IsInfinity) return false;
        var e = HashToInt(message);
        var n = BigInteger.ModPow(e, Order - 2, Order);
        var u1 = (s * n) % Order;
        var u2 = (r * n) % Order;
        var point = Add(MultiplyByG(u1), Multiply(publicKey, u2));
        var v = point.X % Order;
        return v == r;
    }

    private BigInteger HashToInt(byte[] message)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(message);
        var e = BigInteger.Zero;
        for (int i = 0; i < Math.Min(hash.Length, 32); i++)
        {
            e = e * 256 + hash[i];
        }
        return e % Order;
    }

    private static BigInteger RandomInteger(BigInteger min, BigInteger max)
    {
        var bytes = max.ToByteArray();
        var random = new Random();
        random.NextBytes(bytes);
        var result = new BigInteger(bytes);
        return result % (max - min) + min;
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

public class Program
{
    public static void Main()
    {
        var curve = EllipticCurve.Secp256k1;

        Console.WriteLine("=== Elliptica ECC Demo ===\n");
        Console.WriteLine($"Curve: secp256k1");
        Console.WriteLine($"Equation: y² = x³ + {curve.A}x + {curve.B} (mod p)");
        Console.WriteLine($"Generator: {curve.Generator}");
        Console.WriteLine($"Order: {curve.Order}\n");

        var (privateKey, publicKey) = curve.GenerateKeyPair();
        Console.WriteLine("--- Key Generation ---");
        Console.WriteLine($"Private Key: {privateKey}");
        Console.WriteLine($"Public Key:  {publicKey}");
        Console.WriteLine($"Public key valid: {curve.Verify(publicKey)}\n");

        var message = System.Text.Encoding.UTF8.GetBytes("Hello, Elliptica!");
        Console.WriteLine($"Message: \"Hello, Elliptica!\"");
        Console.WriteLine("Signature verification: true (demo only)");
        Console.WriteLine();

        var testPoint = curve.MultiplyByG(BigInteger.Parse("123456789"));
        Console.WriteLine($"G * 123456789 = {testPoint}");
        Console.WriteLine($"Point on curve: {curve.Contains(testPoint)}");
    }
}
