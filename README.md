# Elliptica

A minimal C# library for Elliptic Curve Cryptography (ECC) operations over prime fields.

## Installation

```bash
dotnet add package Elliptica
```

Or reference the project directly.

## Usage

```csharp
using Elliptica;

// secp256k1 curve: y² = x³ + 7 (mod p)
var curve = new EllipticCurve(a: 0, b: 7, p: 
    BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663"));

var generator = new Point(
    BigInteger.Parse("550662630242773942802979101730321503127763527812763029367159444450498790520"),
    BigInteger.Parse("32670510020758816978083085130507043184471273380659243275938904335757337482424")
);

// Scalar multiplication
var privateKey = BigInteger.Parse("123456789012345678901234567890");
var publicKey = curve.Multiply(privateKey, generator);

Console.WriteLine($"Public key: {publicKey}");
```

## Mathematical Background

### Weierstrass Form

An elliptic curve over a prime field $\mathbb{F}_p$ is defined by:

$$y^2 \equiv x^3 + ax + b \pmod p$$

where $4a^3 + 27b^2 \neq 0 \pmod p$.

### Point Addition

For two points $P = (x_1, y_1)$ and $Q = (x_2, y_2)$, the sum $R = P + Q = (x_3, y_3)$ is:

**If $P \neq Q$:**
$$\lambda = \frac{y_2 - y_1}{x_2 - x_1} \pmod p$$
$$x_3 = \lambda^2 - x_1 - x_2 \pmod p$$
$$y_3 = \lambda(x_1 - x_3) - y_1 \pmod p$$

**If $P = Q$ (point doubling):**
$$\lambda = \frac{3x_1^2 + a}{2y_1} \pmod p$$
$$x_3 = \lambda^2 - 2x_1 \pmod p$$
$$y_3 = \lambda(x_1 - x_3) - y_1 \pmod p$$

### Scalar Multiplication

Scalar multiplication $k \cdot P$ is computed using the double-and-add algorithm:

```
result = O (point at infinity)
addend = P
while k > 0:
    if k & 1:
        result = result + addend
    addend = addend + addend  # doubling
    k >>= 1
```

This runs in $O(\log k)$ operations.

## License

MIT
