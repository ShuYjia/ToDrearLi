using System;

/// <summary>
/// Stateless IPv4 validation used by the original M02 YouXian10 page.
/// </summary>
public static class TexCore_ServerInstallIpValidation
{
    public static bool TryValidate(string address, string subnetMask, string gateway, out string error)
    {
        error = string.Empty;
        if (!TryParseIpv4(address, out uint addressValue))
        {
            error = "IP地址格式不正确";
            return false;
        }

        if (!TryParseIpv4(subnetMask, out uint maskValue) || !IsContiguousSubnetMask(maskValue))
        {
            error = "子网掩码格式不正确";
            return false;
        }

        if (!TryParseIpv4(gateway, out uint gatewayValue))
        {
            error = "默认网关格式不正确";
            return false;
        }

        uint network = addressValue & maskValue;
        uint broadcast = network | ~maskValue;
        if (addressValue == network || addressValue == broadcast)
        {
            error = "IP地址不能是网络地址或广播地址";
            return false;
        }

        if (gatewayValue == network || gatewayValue == broadcast || (gatewayValue & maskValue) != network)
        {
            error = "默认网关必须是同一子网内的可用主机地址";
            return false;
        }

        return true;
    }

    private static bool TryParseIpv4(string value, out uint parsed)
    {
        parsed = 0;
        string[] parts = (value ?? string.Empty).Trim().Split('.');
        if (parts.Length != 4) return false;
        foreach (string part in parts)
        {
            if (part.Length == 0 || !int.TryParse(part, out int octet) || octet < 0 || octet > 255)
                return false;
            parsed = (parsed << 8) | (uint)octet;
        }
        return true;
    }

    private static bool IsContiguousSubnetMask(uint mask)
    {
        if (mask == 0 || mask == uint.MaxValue) return false;
        uint inverted = ~mask;
        return (inverted & (inverted + 1)) == 0;
    }
}
