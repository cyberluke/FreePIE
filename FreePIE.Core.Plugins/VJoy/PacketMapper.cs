﻿using FreePIE.Core.Plugins.VJoy.PacketData;
using System;

namespace FreePIE.Core.Plugins.VJoy
{
    public class PacketMapper
    {
        private PacketAction[] mapArr;
        public PacketMapper()
        {
            FFBPType highestValue = VJoyUtils.highestValueInEnum<FFBPType>();
            mapArr = new PacketAction[ (int)highestValue + 1];
            SetupDefaultMap();
        }

        public PacketAction this[FFBPType pt]
        {
            get { return mapArr[(int)pt]; }
            set { mapArr[(int)pt] = value; }
        }

        private void SetupDefaultMap()
        {
            //XXX: For now, PacketAction's can have an empty action, for the sole purpose of converting and printing them out as debug. However, once that's not needed anymore these should be removed to prevent the overhead of unnecessary converting/enqueueing.
            // @TODO: Check that converting to (int) is always ok here

            // Write Reports

            // Usage Set Effect Report
            this[FFBPType.PT_EFFREP] = new PacketAction<EffectReportPacket>((d, p, r) => d.SetEffectParams(p));
            // Usage Set Envelope Report
            this[FFBPType.PT_ENVREP] = new PacketAction<EnvelopeReportPacket>((d, p, r) => d.SetEnvelope(p.BlockIndex, (int)p.Effect.AttackLevel, (int)p.Effect.AttackTime, (int)p.Effect.FadeLevel, (int)p.Effect.FadeTime));
            // Usage Set Condition Report
            this[FFBPType.PT_CONDREP] = new PacketAction<ConditionReportPacket>((d, p ,r) => d.setConditionReport(p.BlockIndex, p.Effect.CenterPointOffset, p.Effect.DeadBand, p.Effect.isY, p.Effect.NegCoeff, (int)p.Effect.NegSatur, p.Effect.PosCoeff, (int)p.Effect.PosSatur));
            // Usage Set Periodic Report
            this[FFBPType.PT_PRIDREP] = new PacketAction<PeriodicReportPacket>((d, p, r) => d.SetPeriodicForce(p.BlockIndex, (int)p.Effect.Magnitude, p.Effect.Offset, (int)p.Effect.Period, (int)p.Effect.Phase));
            // Usage Set Constant Force Report
            this[FFBPType.PT_CONSTREP] = new PacketAction<ConstantForcePacket>((d, p, r) => d.SetConstantForce(p.BlockIndex, p.Effect.Magnitude));
            // Usage Set Ramp Force Report
            this[FFBPType.PT_RAMPREP] = new PacketAction<RampReportPacket>((d, p, r) => d.setRamp(p.BlockIndex, p.Effect.Start, p.Effect.End));
            // Usage Custom Force Data Report
            this[FFBPType.PT_CSTMREP] = new PacketAction<BasePacket>(null);
            // Usage Download Force Sample
            this[FFBPType.PT_SMPLREP] = new PacketAction<BasePacket>((d, p, r) => d.DownloadToDevice(p.BlockIndex));
            // Usage Effect Operation Report
            this[FFBPType.PT_EFOPREP] = new PacketAction<EffectOperationPacket>((d, p, r) => d.OperateEffect(p.BlockIndex, p.Effect.EffectOp, p.Effect.LoopCount));
            // Usage PID Block Free Report
            this[FFBPType.PT_BLKFRREP] = new PacketAction<BasePacket>((d, p, r) => d.DisposeEffect(p.BlockIndex));
            // Usage PID Device Control
            this[FFBPType.PT_CTRLREP] = new PacketAction<PIDDeviceControlPacket>(null);
            // Usage Device Gain Report
            //this[FFBPType.PT_GAINREP] = new PacketAction<DeviceGainPacket>((d, p, r) => d.Gain = p.NormalizedGain);
            this[FFBPType.PT_GAINREP] = new PacketAction<DeviceGainPacket>((d, p, r) => d.Gain = 5000);
            // Usage Set Custom Force Report
            this[FFBPType.PT_SETCREP] = new PacketAction<BasePacket>(null);

            // Feature Reports

            // Usage Create New Effect Report
            this[FFBPType.PT_NEWEFREP] = null; // new PacketAction<CreateNewEffectPacket>((d, p, r) => d.CreateEffect(p.BlockIndex, p.Type));
            // Usage Block Load Report
            this[FFBPType.PT_BLKLDREP] = new PacketAction<DeviceReportPacket>(null);
            // Usage PID Pool Report
            this[FFBPType.PT_POOLREP] = new PacketAction<DeviceReportPacket>(null);
            // Usage PID State Report
            this[FFBPType.PT_STATEREP] = new PacketAction<DeviceReportPacket>(null);
        }
    }
}
