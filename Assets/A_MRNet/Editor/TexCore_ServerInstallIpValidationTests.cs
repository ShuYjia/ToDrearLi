using NUnit.Framework;

public sealed class TexCore_ServerInstallIpValidationTests
{
    [Test]
    public void TryValidate_AcceptsHostAndGatewayOnTheSameSubnet()
    {
        bool valid = TexCore_ServerInstallIpValidation.TryValidate(
            "192.168.10.20",
            "255.255.255.0",
            "192.168.10.1",
            out string error);

        Assert.That(valid, Is.True, error);
        Assert.That(error, Is.Empty);
    }

    [TestCase("192.168.10.256", "255.255.255.0", "192.168.10.1")]
    [TestCase("192.168.10.20", "255.0.255.0", "192.168.10.1")]
    [TestCase("192.168.10.0", "255.255.255.0", "192.168.10.1")]
    [TestCase("192.168.10.20", "255.255.255.0", "10.0.0.1")]
    public void TryValidate_RejectsInvalidAddressCombinations(
        string address,
        string subnetMask,
        string gateway)
    {
        bool valid = TexCore_ServerInstallIpValidation.TryValidate(
            address,
            subnetMask,
            gateway,
            out string error);

        Assert.That(valid, Is.False);
        Assert.That(error, Is.Not.Empty);
    }
}
