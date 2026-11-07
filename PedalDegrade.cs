// Pedal Degrade — sample-rate and bit-depth reducer for ReBuzz.
// Copyright (C) 2026 thepedal. GPL-3.0-or-later; see LICENSE.
//
// Clean-room machine with the same controls as the Arguelles Degrader for
// Jeskola Buzz (Smp. Rate as a whole-number divisor of the host rate, Bit
// Depth 1..16). No code from the original is used.

using System;
using System.Collections.Generic;
using System.Windows;
using Buzz.MachineInterface;
using BuzzGUI.Common;
using BuzzGUI.Interfaces;

namespace Pedal.Degrade
{
    [MachineDecl(Name = "Pedal Degrade", ShortName = "Degrade", Author = "thepedal")]
    public class PedalDegradeMachine : IBuzzMachine
    {
        internal const string Version = "1.0.0";

        const int MaxDivisor = 64;
        const int MaxBits    = 16;   // 16 = no quantisation (Buzz ±32768 scale, Core §38)

        readonly IBuzzMachineHost _host;

        // Hold state carries across Work calls so the hold period is exact
        // regardless of where buffer boundaries fall.
        int   _holdLeft;     // samples remaining in the current hold period
        float _heldL, _heldR;

        // Last host rate seen in Work, for the Hz read-out in DescribeValue
        // (MasterInfo is only valid in Work/setters, IBuzzMachineHost docs).
        volatile int _sampleRate = 44100;

        public PedalDegradeMachine(IBuzzMachineHost host)
        {
            _host = host;
        }

        // ── Parameters ─────────────────────────────────────────────────────
        // Order is fixed: append new parameters at the end (Build §3.3).

        [ParameterDecl(Name = "Smp. Rate", MinValue = 1, MaxValue = MaxDivisor, DefValue = 16,
            Description = "Output sample rate as a divisor of the host rate. 1 is the host rate; 16 at 44.1 kHz is 2756 Hz.")]
        public int SmpRate { get; set; } = 16;

        [ParameterDecl(Name = "Bit Depth", MinValue = 1, MaxValue = MaxBits, DefValue = 6,
            Description = "Quantisation in bits. 16 is clean, 1 is a three-level fuzz.")]
        public int BitDepth { get; set; } = 6;

        // ── Display ────────────────────────────────────────────────────────

        public string? DescribeValue(IParameter parameter, int value)
        {
            switch (parameter?.Name)
            {
                case "Smp. Rate":
                    if (value < 1) return null;
                    return (int)MathF.Round((float)_sampleRate / value) + " Hz";
                case "Bit Depth":
                    return value == 1 ? "1 Bit" : value + " Bits";
                default:
                    return null;
            }
        }

        // ── Audio ──────────────────────────────────────────────────────────

        public bool Work(Sample[] output, Sample[] input, int n, WorkModes mode)
        {
            int sr = _host?.MasterInfo?.SamplesPerSec ?? 0;
            if (sr > 0) _sampleRate = sr;

            int div  = Math.Clamp(SmpRate,  1, MaxDivisor);
            int bits = Math.Clamp(BitDepth, 1, MaxBits);

            // Fully clean: let the host pass the input through untouched.
            // For managed effects, false means the host forwards the input
            // (PedalMasterN §1), so this is a free bypass.
            if (div == 1 && bits == MaxBits)
            {
                _holdLeft = 0;
                return false;
            }

            // Without WM_READ the host leaves the previous chunk in the input
            // buffer (PedalMasterN §1). Treat it as silence.
            if ((mode & WorkModes.WM_READ) == 0)
            {
                Array.Clear(output, 0, n);
                _holdLeft = 0; _heldL = 0f; _heldR = 0f;
                return true;
            }

            // Mid-tread rounding to a power-of-two step. This is an odd
            // function, so it adds no DC (Core §43.1) and silence stays at 0.
            // Both factors are powers of two, so the scaling is exact.
            bool  quantise = bits < MaxBits;
            float step     = (float)(1 << (MaxBits - bits));
            float invStep  = 1f / step;

            if (div == 1)
            {
                // Quantise only, every sample.
                for (int i = 0; i < n; i++)
                {
                    output[i].L = MathF.Round(input[i].L * invStep) * step;
                    output[i].R = MathF.Round(input[i].R * invStep) * step;
                }
                _holdLeft = 0;
                return true;
            }

            // Sample-and-hold. Quantising happens only at each new hold point,
            // so its cost is divided by the divisor.
            int   left = Math.Min(_holdLeft, div);   // divisor may have dropped
            float hl = _heldL, hr = _heldR;

            for (int i = 0; i < n; i++)
            {
                if (left == 0)
                {
                    hl = input[i].L;
                    hr = input[i].R;
                    if (quantise)
                    {
                        hl = MathF.Round(hl * invStep) * step;
                        hr = MathF.Round(hr * invStep) * step;
                    }
                    left = div;
                }
                output[i].L = hl;
                output[i].R = hr;
                left--;
            }

            _holdLeft = left; _heldL = hl; _heldR = hr;
            return true;
        }

        // ── Right-click menu ───────────────────────────────────────────────

        public IEnumerable<IMenuItem> Commands
        {
            get
            {
                yield return new MenuItemVM()
                {
                    Text = "About...",
                    Command = new SimpleCommand()
                    {
                        CanExecuteDelegate = p => true,
                        ExecuteDelegate    = p => MessageBox.Show(
                            $"Pedal Degrade   v{Version}\n\n" +
                            "Sample-rate and bit-depth reducer.\n" +
                            "Controls modelled on the Arguelles Degrader for Jeskola Buzz (clean-room, no original code).\n\n" +
                            "github.com/thepedal/pedal-degrade\n" +
                            "GPL-3.0-or-later",
                            "About Pedal Degrade")
                    }
                };
            }
        }
    }
}
