namespace BF1ModTools.Core;

public static class EaCrypto
{
    private const int prime_10000th = 104729;
    private const int prime_20000th = 224737;
    private const int prime_30000th = 350377;

    public static byte[] GetArray(string strData)
    {
        strData = strData.ToLower();

        var memoryStream = new MemoryStream();
        var stringBuilder = new StringBuilder();

        var source = "0123456789abcdef";

        foreach (char c in strData)
        {
            if (!source.Contains(c))
                continue;

            stringBuilder.Append(c);
        }

        byte[] result;
        if (stringBuilder.Length % 2 != 0)
        {
            result = Array.Empty<byte>();
        }
        else
        {
            strData = stringBuilder.ToString();
            for (int i = 0; i < stringBuilder.Length / 2; i++)
            {
                memoryStream.WriteByte(Convert.ToByte(strData.Substring(i * 2, 2), 16));
            }
            result = memoryStream.ToArray();
        }

        return result;
    }

    public static string ToHex(byte[] data)
    {
        var strBuilder = new StringBuilder();

        foreach (byte b in data)
        {
            strBuilder.Append(b.ToString("x2"));
        }

        return strBuilder.ToString();
    }

    public static string GetRTPHandshakeCode()
    {
        var dateTime = DateTime.UtcNow;

        var year = (uint)dateTime.Year;
        var month = (uint)dateTime.Month;
        var day = (uint)dateTime.Day;

        var temp_value = (prime_10000th * year) ^ (month * prime_20000th) ^ (day * prime_30000th);
        var hashed_timestamp = temp_value ^ (temp_value << 16) ^ (temp_value >> 16);

        return hashed_timestamp.ToString();
    }

    public static Aes GetAesDataByKey(byte[] key)
    {
        var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        return aes;
    }

    public static string Decrypt(byte[] decrypt)
    {
        using var rDel = Aes.Create("AesManaged");
        rDel.IV = new byte[16];
        rDel.Key = new byte[] { 65, 50, 114, 45, 208, 130, 239, 176, 220, 100, 87, 197, 118, 104, 202, 9 };
        rDel.Mode = CipherMode.CBC;
        rDel.Padding = PaddingMode.None;

        var cryptoTransform = rDel.CreateDecryptor();
        var decrypted = cryptoTransform.TransformFinalBlock(decrypt, 0, decrypt.Length);

        return Encoding.UTF8.GetString(decrypted);
    }

    public static string Decrypt(Aes aes, byte[] data)
    {
        try
        {
            var iCryptoTransform = aes.CreateDecryptor();
            var memoryStream = new MemoryStream();

            using var cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();

            return Encoding.ASCII.GetString(memoryStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string Encrypt(Aes aes, byte[] data)
    {
        try
        {
            var iCryptoTransform = aes.CreateEncryptor();
            var memoryStream = new MemoryStream();

            using var cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();

            return ToHex(memoryStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool CheckChallengeResponse(string response, string key)
    {
        try
        {
            var array = GetArray(response);
            var aes = GetAesDataByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

            return Decrypt(aes, array) == key;
        }
        catch
        {
            return false;
        }
    }

    public static string MakeChallengeResponse(string key)
    {
        var aes = GetAesDataByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        var bytes = Encoding.ASCII.GetBytes(key);

        return Encrypt(aes, bytes);
    }

    public static byte[] GetLSXKey(ushort seed)
    {
        var crandom = new CRandom();
        crandom.Seed(7u);
        crandom.Seed((uint)(crandom.Rand() + seed));

        var array = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            array[i] = (byte)crandom.Rand();
        }

        return array;
    }

    public static string LSXDecryptBF4(string data, ushort seed)
    {
        var key = GetLSXKey(seed);

        var aes = GetAesDataByKey(key);
        var array = GetArray(data);

        return Decrypt(aes, array);
    }

    public static string LSXEncryptBF4(string data, ushort seed)
    {
        var key = GetLSXKey(seed);

        var aes = GetAesDataByKey(key);
        var bytes = Encoding.UTF8.GetBytes(data);

        return Encrypt(aes, bytes);
    }
}
