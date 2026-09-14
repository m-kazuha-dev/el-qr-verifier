namespace ElQrVerifier.Web.Services
{
    public class ElQrVerificationResult
    {
        public bool IsSuccess => IsCd1Valid && IsCd2Valid && IsCd3Valid;
        public string RawData { get; set; } = "";
        public string PaymentAmount { get; set; } = "";
        public string DueDate { get; set; } = "";
        
        public string ExpectedCd1 { get; set; } = "";
        public string ActualCd1 { get; set; } = "";
        public bool IsCd1Valid => ExpectedCd1 == ActualCd1;

        public string ExpectedCd2 { get; set; } = "";
        public string ActualCd2 { get; set; } = "";
        public bool IsCd2Valid => ExpectedCd2 == ActualCd2;

        public string ExpectedCd3 { get; set; } = "";
        public string ActualCd3 { get; set; } = "";
        public bool IsCd3Valid => ExpectedCd3 == ActualCd3;
    }

    public static class ElQrValidator
    {
        private const ushort Poly = 0x1021;
        private const ushort Init = 0xFFFF;
        private static readonly ushort[] CrcTable = new ushort[256];

        static ElQrValidator()
        {
            for (int i = 0; i < 256; i++)
            {
                ushort r = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    r = (r & 0x8000) != 0 ? (ushort)((r << 1) ^ Poly) : (ushort)(r << 1);
                }
                CrcTable[i] = r;
            }
        }

        public static ElQrVerificationResult ParseAndVerify(string qrData)
        {
            if (string.IsNullOrEmpty(qrData) || qrData.Length != 255)
            {
                throw new ArgumentException($"QRコードのデータは255桁である必要があります。（現在: {qrData?.Length ?? 0}桁）");
            }

            var result = new ElQrVerificationResult
            {
                RawData = qrData,
                ActualCd1 = qrData.Substring(29, 2),
                ActualCd2 = qrData.Substring(68, 2),
                ActualCd3 = qrData.Substring(250, 5),
                PaymentAmount = qrData.Substring(42, 11).TrimStart('0'),
                DueDate = qrData.Substring(141, 8)
            };

            // CD1計算 (index: 31, length: 37)
            result.ExpectedCd1 = CalculateMpnCheckDigit(qrData.Substring(31, 37));

            // CD2計算 (index: 70, length: 42)
            result.ExpectedCd2 = CalculateMpnCheckDigit(qrData.Substring(70, 42));

            // CD3計算 (CRC-16: index 0, length 250)
            result.ExpectedCd3 = CalculateCrc16(qrData.Substring(0, 250));

            return result;
        }

        private static string CalculateMpnCheckDigit(string rawNumericData)
        {
            int sum1 = 0;
            int weightCounter1 = 0;
            for (int i = 0; i < rawNumericData.Length; i++)
            {
                if ((i + 1) % 2 != 0)
                {
                    int weight = 9 - (weightCounter1 % 8);
                    sum1 += (rawNumericData[i] - '0') * weight;
                    weightCounter1++;
                }
            }
            int cd2nd = sum1 % 10;

            string extendedData = cd2nd.ToString() + rawNumericData;
            int sum2 = 0;
            int weightCounter2 = 0;
            for (int i = 0; i < extendedData.Length; i++)
            {
                if ((i + 1) % 2 != 0)
                {
                    int weight = 2 + (weightCounter2 % 8);
                    sum2 += (extendedData[i] - '0') * weight;
                    weightCounter2++;
                }
            }
            int cd1st = sum2 % 11;
            if (cd1st == 10) cd1st = 0;

            return $"{cd1st}{cd2nd}";
        }

        private static string CalculateCrc16(string asciiPayload)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(asciiPayload);
            ushort crc = Init;

            foreach (byte b in data)
            {
                crc = (ushort)(CrcTable[((crc >> 8) ^ b) & 0xFF] ^ (crc << 8));
            }

            return crc.ToString("D5");
        }
    }
}