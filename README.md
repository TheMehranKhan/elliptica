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

// Use secp256k1 curve (Bitcoin/Ethereum standard)
var curve = EllipticCurve.Secp256k1;

// Generate key pair
var (privateKey, publicKey) = curve.GenerateKeyPair();
Console.WriteLine($"Private: {privateKey}");
Console.WriteLine($"Public:  {publicKey}");

// Verify public key is on curve
bool valid = curve.Verify(publicKey);

// Custom curve
var custom = new EllipticCurve(
    a: 0, b: 7, p: BigInteger.Parse("..."),
    generator: new Point(x, y),
    order: BigInteger.Parse("...")
);

// Scalar multiplication
var point = curve.MultiplyByG(privateKey);
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
