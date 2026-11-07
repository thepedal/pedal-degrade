# Pedal Degrade

A small, very cheap sample-rate and bit-depth reducer for ReBuzz. It has the
same two controls as the Arguelles Degrader for Jeskola Buzz. This is a
clean-room machine: no code from the original is used.

Version 1.0.0 · managed effect · net10.0-windows · GPL-3.0-or-later

## Parameters

| Name | Range | Default | What it does |
|---|---|---|---|
| Smp. Rate | 1–64 | 16 | Divides the host rate. The read-out shows the result in Hz for the current host rate (16 at 44.1 kHz is 2756 Hz). 1 means no reduction. |
| Bit Depth | 1–16 | 6 | Rounds each sample to 2^bits levels across full scale. 16 is clean. 1 leaves three levels (−FS, 0, +FS), a gated square fuzz. |

The amp and pan rows above these in the parameter window belong to the input
connection, not to the machine.

## How it works

- **Sample-and-hold.** Every N samples it takes a new input value and holds it
  for N samples. There's no anti-alias filter, on purpose. The hold position is
  kept between buffers, so the period stays exact whatever the buffer size.
- **Quantising.** Mid-tread rounding to a power-of-two step. Rounding is
  symmetric, so it adds no DC offset and silence stays exactly silent.
- **Cost.** Quantising happens only once per hold period. At the default
  settings it costs under 1 ns per sample (about 0.003% of one core at
  44.1 kHz). Work allocates nothing.
- **Bypass.** At Smp. Rate 1 and Bit Depth 16, Work returns false. The host
  then passes the input through untouched and nothing is processed.

## Presets

`Pedal Degrade.prs.xml` loads automatically when the machine is created:
Clean, Classic Degrade, 12-bit Sampler, 8-bit Console, Walkie Crunch, 4-bit
Grit, 1-bit Fuzz, Aliased Shimmer and Subterranean.
`gen_presets.py` generates the bundle. Run it with `--check` to check the
parameter order and ranges against the source. It isn't deployed.

## Build

```
dotnet build -c Release
```

The build copies `Pedal Degrade.NET.dll` and the preset bundle to
`C:\Program Files\ReBuzz\Gear\Effects\`. Close ReBuzz first, because it locks
the DLL. If ReBuzz is installed somewhere else, use `-p:ReBuzzPath=<dir>`.
After deploying, check that the version under right-click → About... is the
new one.
