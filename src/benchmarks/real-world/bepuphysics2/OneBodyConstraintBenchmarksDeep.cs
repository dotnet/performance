﻿using BenchmarkDotNet.Attributes;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using System.Numerics;
using static DemoBenchmarks.OneBodyConstraintBenchmarks;

namespace DemoBenchmarks;

/// <summary>
/// Evaluates performance of all one body constraints excluded from <see cref="OneBodyConstraintBenchmarks"/>
/// </summary>
/// <remarks>
/// Note that all constraints operate across <see cref="Vector{}.Count"/> lanes simultaneously where T is of type <see cref="float"/>.
/// <para>The number of bundles being executed does not change if <see cref="Vector{}.Count"/> changes; if larger bundles are allowed, then more lanes end up getting solved.</para>
/// </remarks>
public class OneBodyConstraintBenchmarksDeep
{
    [Benchmark]
    public BodyVelocityWide Contact1OneBody()
    {
        var prestep = new Contact1OneBodyPrestepData
        {
            Contact0 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            MaterialProperties = new MaterialPropertiesWide
            {
                FrictionCoefficient = new Vector<float>(1f),
                MaximumRecoveryVelocity = new Vector<float>(2f),
                SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
            },
            Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0))
        };

        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<Contact1OneBodyFunctions, Contact1OneBodyPrestepData, Contact1AccumulatedImpulses>(new Vector3Wide(), orientation, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide Contact2OneBody()
    {
        var prestep = new Contact2OneBodyPrestepData
        {
            Contact0 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            Contact1 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            MaterialProperties = new MaterialPropertiesWide
            {
                FrictionCoefficient = new Vector<float>(1f),
                MaximumRecoveryVelocity = new Vector<float>(2f),
                SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
            },
            Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0))
        };

        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<Contact2OneBodyFunctions, Contact2OneBodyPrestepData, Contact2AccumulatedImpulses>(new Vector3Wide(), orientation, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide Contact3OneBody()
    {
        var prestep = new Contact3OneBodyPrestepData
        {
            Contact0 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            Contact1 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            Contact2 = new() { Depth = Vector<float>.Zero, OffsetA = Vector3Wide.Broadcast(new Vector3(1, 0, 0)) },
            MaterialProperties = new MaterialPropertiesWide
            {
                FrictionCoefficient = new Vector<float>(1f),
                MaximumRecoveryVelocity = new Vector<float>(2f),
                SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
            },
            Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0))
        };

        QuaternionWide.Broadcast(Quaternion.Identity, out var orientationA);
        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<Contact3OneBodyFunctions, Contact3OneBodyPrestepData, Contact3AccumulatedImpulses>(new Vector3Wide(), orientationA, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide Contact2NonconvexOneBody()
    {
        var prestep = new Contact2NonconvexOneBodyPrestepData
        {
            Contact0 = new() { Depth = Vector<float>.Zero, Offset = Vector3Wide.Broadcast(new Vector3(1, 0, 0)), Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0)) },
            Contact1 = new() { Depth = Vector<float>.Zero, Offset = Vector3Wide.Broadcast(new Vector3(1, 0, 0)), Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0)) },
            MaterialProperties = new MaterialPropertiesWide
            {
                FrictionCoefficient = new Vector<float>(1f),
                MaximumRecoveryVelocity = new Vector<float>(2f),
                SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
            },
        };

        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<ContactNonconvexOneBodyFunctions<Contact2NonconvexOneBodyPrestepData, Contact2NonconvexAccumulatedImpulses>,
            Contact2NonconvexOneBodyPrestepData, Contact2NonconvexAccumulatedImpulses>(new Vector3Wide(), orientation, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide Contact3NonconvexOneBody()
    {
        var prestep = new Contact3NonconvexOneBodyPrestepData
        {
            Contact0 = new() { Depth = Vector<float>.Zero, Offset = Vector3Wide.Broadcast(new Vector3(1, 0, 0)), Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0)) },
            Contact1 = new() { Depth = Vector<float>.Zero, Offset = Vector3Wide.Broadcast(new Vector3(1, 0, 0)), Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0)) },
            Contact2 = new() { Depth = Vector<float>.Zero, Offset = Vector3Wide.Broadcast(new Vector3(1, 0, 0)), Normal = Vector3Wide.Broadcast(new Vector3(0, 1, 0)) },
            MaterialProperties = new MaterialPropertiesWide
            {
                FrictionCoefficient = new Vector<float>(1f),
                MaximumRecoveryVelocity = new Vector<float>(2f),
                SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
            },
        };

        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<ContactNonconvexOneBodyFunctions<Contact3NonconvexOneBodyPrestepData, Contact3NonconvexAccumulatedImpulses>,
            Contact3NonconvexOneBodyPrestepData, Contact3NonconvexAccumulatedImpulses>(new Vector3Wide(), orientation, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide OneBodyAngularMotor()
    {
        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var prestep = new OneBodyAngularMotorPrestepData
        {
            Settings = new() { Damping = Vector<float>.One, MaximumForce = Vector<float>.One },
            TargetVelocity = default
        };

        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<OneBodyAngularMotorFunctions, OneBodyAngularMotorPrestepData, Vector3Wide>(new Vector3Wide(), orientation, inertia, prestep);
    }

    [Benchmark]
    public BodyVelocityWide OneBodyLinearServo()
    {
        QuaternionWide.Broadcast(Quaternion.Identity, out var orientation);
        var prestep = new OneBodyLinearServoPrestepData
        {
            ServoSettings = new() { BaseSpeed = Vector<float>.Zero, MaximumForce = new Vector<float>(float.MaxValue), MaximumSpeed = new Vector<float>(float.MaxValue) },
            SpringSettings = new() { TwiceDampingRatio = new Vector<float>(2), AngularFrequency = new Vector<float>(20 * MathF.PI) }
        };

        var inertia = new BodyInertiaWide { InverseInertiaTensor = new Symmetric3x3Wide { XX = Vector<float>.One, YY = Vector<float>.One, ZZ = Vector<float>.One }, InverseMass = Vector<float>.One };
        return BenchmarkOneBodyConstraint<OneBodyLinearServoFunctions, OneBodyLinearServoPrestepData, Vector3Wide>(new Vector3Wide(), orientation, inertia, prestep);
    }
}
