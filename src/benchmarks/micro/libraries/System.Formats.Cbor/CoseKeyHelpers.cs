// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Test.Cryptography;

// Provides a reference implementation for serializing ECDsa public keys to the COSE_Key format
// according to https://tools.ietf.org/html/rfc8152#section-8.1

namespace System.Formats.Cbor.Tests
{
    public class ECDsaCosePublicKey
    {
        public ECDsaCosePublicKey(string curveFriendlyName, string hexQx, string hexQy, string hashAlgorithmName, string hexEncodedKey)
        {
            Name = curveFriendlyName;
            HashAlgorithmName = new HashAlgorithmName(hashAlgorithmName);
            EncodedCoseKey = hexEncodedKey.HexToByteArray();
            ECParameters = new ECParameters()
            {
                Curve = ECCurve.CreateFromFriendlyName(curveFriendlyName),
                Q = new ECPoint() { X = hexQx.HexToByteArray(), Y = hexQy.HexToByteArray() },
            };
        }

        public string Name { get; }
        public ECParameters ECParameters { get; }
        public HashAlgorithmName HashAlgorithmName { get; }
        public byte[] EncodedCoseKey { get; }

        public override string ToString() => Name;

        public static IEnumerable<ECDsaCosePublicKey> CreatePublicKeys()
        {
            yield return new ECDsaCosePublicKey(
                curveFriendlyName: "ECDSA_P256",
                hexQx: "65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d",
                hexQy: "1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                hexEncodedKey: "a501020326200121582065eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d2258201e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                hashAlgorithmName: "SHA256");

            yield return new ECDsaCosePublicKey(
                curveFriendlyName: "ECDSA_P384",
                hexQx: "ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd",
                hexQy: "24376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                hexEncodedKey: "a501020338222002215830ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd22583024376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                hashAlgorithmName: "SHA384");

            yield return new ECDsaCosePublicKey(
                curveFriendlyName: "ECDSA_P521",
                hexQx: "00b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c",
                hexQy: "0114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                hexEncodedKey: "a50102033823200321584200b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c2258420114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                hashAlgorithmName: "SHA512");
        }
    }

    public static class ECDsaCosePublicKeyHelper
    {
        public static void WriteECParametersAsCosePublicKey(this CborWriter writer, ECParameters ecParams, HashAlgorithmName? algorithmName)
        {
            Debug.Assert(writer.ConformanceMode == CborConformanceMode.Ctap2Canonical && writer.ConvertIndefiniteLengthEncodings);

            if (ecParams.Q.X is null || ecParams.Q.Y is null)
            {
                throw new ArgumentException("does not specify a public key point.", nameof(ecParams));
            }

            // run these first to perform necessary validation
            (CoseKeyType kty, CoseCrvId crv) = MapECCurveToCoseKtyAndCrv(ecParams.Curve);
            CoseKeyAlgorithm? alg = (algorithmName != null) ? MapHashAlgorithmNameToCoseKeyAlg(algorithmName.Value) : (CoseKeyAlgorithm?)null;

            // Begin writing a CBOR object
            writer.WriteStartMap(definiteLength: null);

            // NB labels should be sorted according to CTAP2 canonical encoding rules.
            // While the CborWriter will attempt to sort the encodings on its own,
            // it is generally more efficient if keys are written in sorted order to begin with.

            WriteCoseKeyLabel(writer, CoseKeyLabel.Kty);
            writer.WriteInt32((int)kty);

            if (alg != null)
            {
                WriteCoseKeyLabel(writer, CoseKeyLabel.Alg);
                writer.WriteInt32((int)alg);
            }

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcCrv);
            writer.WriteInt32((int)crv);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcX);
            writer.WriteByteString(ecParams.Q.X);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcY);
            writer.WriteByteString(ecParams.Q.Y);

            writer.WriteEndMap();

            static (CoseKeyType, CoseCrvId) MapECCurveToCoseKtyAndCrv(ECCurve curve)
            {
                if (!curve.IsNamed)
                {
                    throw new ArgumentException("EC COSE keys only support named curves.", nameof(curve));
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP256))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P256);
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP384))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P384);
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP521))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P521);
                }

                throw new ArgumentException("Unrecognized named curve", curve.Oid.Value);

                bool MatchesOid(ECCurve namedCurve) => curve.Oid.Value == namedCurve.Oid.Value;
            }

            static CoseKeyAlgorithm MapHashAlgorithmNameToCoseKeyAlg(HashAlgorithmName name)
            {
                if (MatchesName(HashAlgorithmName.SHA256))
                {
                    return CoseKeyAlgorithm.ES256;
                }

                if (MatchesName(HashAlgorithmName.SHA384))
                {
                    return CoseKeyAlgorithm.ES384;
                }

                if (MatchesName(HashAlgorithmName.SHA512))
                {
                    return CoseKeyAlgorithm.ES512;
                }

                throw new ArgumentException("Unrecognized hash algorithm name.", nameof(HashAlgorithmName));

                bool MatchesName(HashAlgorithmName candidate) => name.Name == candidate.Name;
            }

            static void WriteCoseKeyLabel(CborWriter writer, CoseKeyLabel label)
            {
                writer.WriteInt32((int)label);
            }
        }

        public static (ECParameters, HashAlgorithmName?) ReadECParametersAsCosePublicKey(this CborReader reader)
        {
            Debug.Assert(reader.ConformanceMode == CborConformanceMode.Ctap2Canonical);

            // CTAP2 conformance mode requires that fields are sorted by key encoding.
            // We take advantage of this by reading keys in that order.
            // NB1. COSE labels are not sorted according to canonical integer ordering,
            //      negative labels must always follow positive labels. 
            // NB2. Any unrecognized keys will result in the reader failing.
            // NB3. in order to support optional fields, we need to store the latest read label.
            CoseKeyLabel? latestReadLabel = null;

            int? remainingKeys = reader.ReadStartMap();
            Debug.Assert(remainingKeys != null); // guaranteed by CTAP2 conformance

            try
            {
                var ecParams = new ECParameters();

                ReadCoseKeyLabel(CoseKeyLabel.Kty);
                CoseKeyType kty = (CoseKeyType)reader.ReadInt32();

                HashAlgorithmName? algName = null;
                if (TryReadCoseKeyLabel(CoseKeyLabel.Alg))
                {
                    CoseKeyAlgorithm alg = (CoseKeyAlgorithm)reader.ReadInt32();
                    algName = MapCoseKeyAlgToHashAlgorithmName(alg);
                }

                if (TryReadCoseKeyLabel(CoseKeyLabel.KeyOps))
                {
                    // No-op, simply tolerate potential key_ops labels
                    reader.SkipValue();
                }

                ReadCoseKeyLabel(CoseKeyLabel.EcCrv);
                CoseCrvId crv = (CoseCrvId)reader.ReadInt32();

                if (IsValidKtyCrvCombination(kty, crv))
                {
                    ecParams.Curve = MapCoseCrvToECCurve(crv);
                }
                else
                {
                    throw new CborContentException("Invalid kty/crv combination in COSE key.");
                }

                ReadCoseKeyLabel(CoseKeyLabel.EcX);
                ecParams.Q.X = reader.ReadByteString();

                ReadCoseKeyLabel(CoseKeyLabel.EcY);
                ecParams.Q.Y = reader.ReadByteString();

                if (TryReadCoseKeyLabel(CoseKeyLabel.EcD))
                {
                    throw new CborContentException("COSE key encodes a private key.");
                }

                if (remainingKeys > 0)
                {
                    throw new CborContentException("COSE_key contains unrecognized trailing data.");
                }

                reader.ReadEndMap();

                return (ecParams, algName);
            }
            catch (InvalidOperationException e)
            {
                throw new CborContentException("Invalid COSE_key format in CBOR document", e);
            }

            static bool IsValidKtyCrvCombination(CoseKeyType kty, CoseCrvId crv)
            {
                return kty switch
                {
                    CoseKeyType.EC2 => crv == CoseCrvId.P256 || crv == CoseCrvId.P384 || crv == CoseCrvId.P521,
                    CoseKeyType.OKP => crv == CoseCrvId.X255519 || crv == CoseCrvId.X448 || crv == CoseCrvId.Ed25519 || crv == CoseCrvId.Ed448,
                    _ => false,
                };
            }

            static ECCurve MapCoseCrvToECCurve(CoseCrvId crv)
            {
                return crv switch
                {
                    CoseCrvId.P256 => ECCurve.NamedCurves.nistP256,
                    CoseCrvId.P384 => ECCurve.NamedCurves.nistP384,
                    CoseCrvId.P521 => ECCurve.NamedCurves.nistP521,
                    _ => throw new CborContentException("Unrecognized COSE crv value."),
                };
            }

            static HashAlgorithmName MapCoseKeyAlgToHashAlgorithmName(CoseKeyAlgorithm alg)
            {
                return alg switch
                {
                    CoseKeyAlgorithm.ES256 => HashAlgorithmName.SHA256,
                    CoseKeyAlgorithm.ES384 => HashAlgorithmName.SHA384,
                    CoseKeyAlgorithm.ES512 => HashAlgorithmName.SHA512,
                    _ => throw new CborContentException("Unrecognized COSE alg value."),
                };
            }

            // Handles optional labels
            bool TryReadCoseKeyLabel(CoseKeyLabel expectedLabel)
            {
                // The `currentLabel` parameter can hold a label that
                // was read when handling a previous optional field.
                // We only need to read the next label if uninhabited.
                if (latestReadLabel == null)
                {
                    // check that we have not reached the end of the COSE key object
                    if (remainingKeys == 0)
                    {
                        return false;
                    }

                    latestReadLabel = (CoseKeyLabel)reader.ReadInt32();
                }

                if (expectedLabel != latestReadLabel.Value)
                {
                    return false;
                }

                // read was successful, vacate the `currentLabel` parameter to advance reads.
                latestReadLabel = null;
                remainingKeys--;
                return true;
            }

            // Handles required labels
            void ReadCoseKeyLabel(CoseKeyLabel expectedLabel)
            {
                if (!TryReadCoseKeyLabel(expectedLabel))
                {
                    throw new CborContentException("Unexpected COSE key label.");
                }
            }
        }

        private enum CoseKeyLabel : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-7.1 table 3
            Kty = 1,
            Kid = 2,
            Alg = 3,
            KeyOps = 4,
            BaseIv = 5,

            // cf. https://tools.ietf.org/html/rfc8152#section-13.1.1 table 23
            EcCrv = -1,
            EcX = -2,
            EcY = -3,
            EcD = -4,
        };

        private enum CoseCrvId : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-13.1 table 22
            P256 = 1,
            P384 = 2,
            P521 = 3,
            X255519 = 4,
            X448 = 5,
            Ed25519 = 6,
            Ed448 = 7,
        }

        private enum CoseKeyType : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-13 table 21
            OKP = 1,
            EC2 = 2,

            Symmetric = 4,
            Reserved = 0,
        }

        private enum CoseKeyAlgorithm : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-8.1 table 5
            ES256 = -7,
            ES384 = -35,
            ES512 = -36,
        }
    }
}
