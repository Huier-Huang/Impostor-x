using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Impostor.Api.Config;

public class ListenerConfig
{
    public const string Section = nameof(ListenerConfig);

    public string CertificateString = string.Empty;

    public string PrivateKeyString = string.Empty;

    public bool EnabledAuthListener { get; set; } = false;

    public bool EnabledDtlListener { get; set; } = false;

    public bool EnabledUDPAuthListener { get; set; } = false;

    public string CertificatePath { get; set; } = string.Empty;

    public string PrivateKeyPath { get; set; } = string.Empty;

    public void ReadCertificate(IServiceCollection service)
    {
        CertificatePath = CertificatePath == string.Empty
            ? Path.Combine(Directory.GetCurrentDirectory(), "Certificate.txt")
            : CertificatePath;
        PrivateKeyPath = PrivateKeyPath == string.Empty
            ? Path.Combine(Directory.GetCurrentDirectory(), "PrivateKey.txt")
            : PrivateKeyPath;

        try
        {
            CertificateString = File.ReadAllText(CertificatePath);

            PrivateKeyString = File.ReadAllText(PrivateKeyPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
