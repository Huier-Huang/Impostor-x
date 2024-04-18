using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Impostor.Server.Utils;

public static class CertificateUtils
{
    public static byte[] DecodePEM(string pemData)
    {
        var result = new List<byte>();

        pemData = pemData.Replace("\r", "");
        var lines = pemData.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("-----"))
            {
                continue;
            }

            var lineData = Convert.FromBase64String(line);
            result.AddRange(lineData);
        }

        return result.ToArray();
    }

    public static RSA DecodeRSAKeyFromPEM(string pemData)
    {
        var pemReader = new PemReader(new StringReader(pemData));
        var parameters = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)pemReader.ReadObject());
        var rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        return rsa;
    }
}
