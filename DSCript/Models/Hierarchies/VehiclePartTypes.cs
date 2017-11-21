using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    public enum VehiclePartType : short
    {
        Generic                 = 1,
        Wheel                   = 2,
        Marker                  = 3,
        Root                    = 4,
        Damage                  = 8,
        DamageExtra             = 9,
    }

    public enum VehiclePartSlotType : short
    {
        Generic                 = 0x00,
            
        Hood                    = 0x04,
        Trunk                   = 0x05,

        ExhaustSmokeEmitter     = 0x06,
        EngineSmokeEmitter      = 0x07,

        CameraTargetExternal    = 0x08,

        DriverSeat              = 0x0A,
        PassengerSeat           = 0x0B,

        SignalLeft              = 0x0C,
        SignalRight             = 0x0D,

        CameraBumperRear        = 0x10,
        CameraPositionExternal  = 0x11,
        CameraBumperFront       = 0x12,

        CameraDashboard         = 0x13,

        CameraWheelLeft         = 0x14,
        CameraWheelRight        = 0x15,

        CameraInteriorUnknown   = 0x16,

        CameraFirstPerson       = 0x17,

        WheelFrontLeft          = 0x1B,
        WheelFrontRight         = 0x1C,
        WheelRearLeft           = 0x1D,
        WheelRearRight          = 0x1E,

        DoorFrontLeft           = 0x1F,
        DoorFrontRight          = 0x20,

        FenderLeft              = 0x21,
        FenderRight             = 0x22,

        BumperFront             = 0x23,
        BumperRear              = 0x24,

        BodyFront               = 0x25,
        BodyMiddle              = 0x26,
        BodyRear                = 0x27,

        MirrorLeft              = 0x28,
        MirrorRight             = 0x29,

        WheelRearLeftExtra      = 0x2A,
        WheelRearRightExtra     = 0x2B,

        TrailerJack             = 0x2D,

        BrakelightLeft          = 0x2E,
        BrakelightRight         = 0x2F,

        ReverseLightLeft        = 0x30,
        ReverseLightRight       = 0x31,

        HeadlightLeft           = 0x32,
        HeadlightRight          = 0x33,

        DoorRearLeftExtra       = 0x34,
        DoorRearRightExtra      = 0x35,

        DoorRearLeft            = 0x36,
        DoorRearRight           = 0x37,

        MotorcycleFork          = 0x3A,
        MotorcycleClutch        = 0x3B,
        MotorcycleHandlebars    = 0x3C,

        Ramp                    = 0x3D,

        TrailerDoorLeft         = 0x3E,
        TrailerDoorRight        = 0x3F,

        CargoDoorLeft           = 0x40,
        CargoDoorRight          = 0x41,

        TrailerUnknown1         = 0x42,
        TrailerUnknown2         = 0x43,
        TrailerUnknown3         = 0x44,

        WheelDamaged            = 0x48,
        WheelDamagedExtra       = 0x4A,

        TailLightLeft           = 0x4E,
        TailLightRight          = 0x4F,

        SirenLeft               = 0x50,
        SirenRight              = 0x52,

        TrainAxleFront          = 0x54,
        TrainAxleRear           = 0x55,

        TrailerContainer        = 0x56,

        ForkliftHoist           = 0x57,
        ForkliftLoader          = 0x59,

        BoatRotorLeft           = 0x5A,
        BoatRotorRight          = 0x5B,

        FrontGrille             = 0x70,
        CornerBumper            = 0x71,
    }
}
