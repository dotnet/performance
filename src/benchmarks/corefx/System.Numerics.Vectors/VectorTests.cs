// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Numerics.Tests
{
    public static class VectorTests
    {
        public const int DefaultInnerIterationsCount = 100000000;

        public const float SinglePositiveDelta = 1.0f / DefaultInnerIterationsCount;

        public const float SingleNegativeDelta = -1.0f / DefaultInnerIterationsCount;

        public static readonly Vector2 Vector2Delta = new Vector2(SinglePositiveDelta, SingleNegativeDelta);

        public static readonly Vector2 Vector2Value = new Vector2(-1.0f, 1.0f);

        public static readonly Vector2 Vector2ValueInverted = new Vector2(1.0f, -1.0f);

        public static readonly Vector3 Vector3Delta = new Vector3(SinglePositiveDelta, SingleNegativeDelta, SinglePositiveDelta);

        public static readonly Vector3 Vector3Value = new Vector3(-1.0f, 1.0f, -1.0f);

        public static readonly Vector3 Vector3ValueInverted = new Vector3(1.0f, -1.0f, 1.0f);

        public static readonly Vector4 Vector4Delta = new Vector4(SinglePositiveDelta, SingleNegativeDelta, SinglePositiveDelta, SingleNegativeDelta);

        public static readonly Vector4 Vector4Value = new Vector4(-1.0f, 1.0f, -1.0f, 1.0f);

        public static readonly Vector4 Vector4ValueInverted = new Vector4(1.0f, -1.0f, 1.0f, -1.0f);
    }
}
