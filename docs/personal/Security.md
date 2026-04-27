Wir speichern hoch relevante informationen von kunden.
Und zwar authentication tokens fuer deren integrationen, api keys.

# TLDR

asnscheinnend doch gut reicht erstmal so aber brauche mehr wissen verstaendnis dafuer

# Envelope Encryption

Wenn euer System den Key braucht, um sich im Namen des Users bei einem Drittanbieter (z.B. Stripe oder AWS) anzumelden, müsst ihr verschlüsseln. Der heutige Standard dafür ist Envelope Encryption.

DEK (Data Encryption Key): Für jeden API-Key in der Datenbank generiert ihr einen eigenen, zufälligen AES-256 Schlüssel. Mit diesem verschlüsselt ihr den API-Key.

KEK (Key Encryption Key): Dieser DEK wird nun mit einem "Meisterschlüssel" verschlüsselt, der in einem KMS (Key Management Service) liegt (z.B. Azure Key Vault, AWS KMS oder HashiCorp Vault).

Speicherung: Ihr speichert in der Datenbank-Zeile: [Verschlüsselter API-Key] + [Verschlüsselter DEK].

Warum ist das State of the Art?

Selbst wenn jemand die Datenbank und den Code hat, fehlen ihm die Hardware-Sicherheitsmodule (HSM) des Cloud-Anbieters, um den DEK zu entschlüsseln.

Ihr müsst nie den "großen" Meisterschlüssel über das Netzwerk schicken.

# Zusätzliche Sicherheits-Layer

Egal welche Verschlüsselung ihr nutzt, solltet ihr folgende Best Practices implementieren:
Feature Beschreibung
Masking Speichert zusätzlich die letzten 4 Stellen des Keys im Klartext (z.B. \*\*\*\*...abcd), damit der User in eurem UI sieht, welchen Key er nutzt, ohne dass ihr ihn entschlüsseln müsst.
Audit Logging Protokolliert jedes Mal, wenn eine Identität den Unprotect-Vorgang aufruft (Rate Limiting!).
Secret Management Nutzt HashiCorp Vault. Es ist das Schweizer Taschenmesser für Secrets und kann Verschlüsselung als Service anbieten ("Transit Engine"), ohne dass eure App die Schlüssel je selbst sieht.

# Impl

https://github.com/jakeswenson/envelop/tree/oxidation/csharp/AppEncryption

nstallation

You can get the latest release from Nuget:

<ItemGroup>
    <PackageReference Include="GoDaddy.Asherah.AppEncryption" Version="0.2.2" />
</ItemGroup>

GoDaddy.Asherah.AppEncryption targets NetStandard 2.0 and NetStandard 2.1. See the .NET Standard documentation and Multi-targeting for more information.
Quick Start

// Create a session factory. The builder steps used below are for testing only.
using (SessionFactory sessionFactory = SessionFactory
.NewBuilder("some_product", "some_service")
.WithMemoryPersistence()
.WithNeverExpiredCryptoPolicy()
.WithStaticKeyManagementService("thisIsAStaticMasterKeyForTesting")
.Build())
{
// Now create a cryptographic session for a partition.
using (Session<byte[], byte[]> sessionBytes =
sessionFactory.GetSessionBytes("some_partition"))
{
// Encrypt some data
const string originalPayloadString = "mysupersecretpayload";
byte[] dataRowRecordBytes = sessionBytes.Encrypt(Encoding.UTF8.GetBytes(originalPayloadString));

        // Decrypt the data
        string decryptedPayloadString = Encoding.UTF8.GetString(sessionBytes.Decrypt(dataRowRecordBytes));
    }

}

A more extensive example is the Reference Application, which will evolve along with the SDK.
