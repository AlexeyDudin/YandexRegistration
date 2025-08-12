namespace YandexRegistrationModel
{
    public static class NameHelper
    {
        private const string _firstNameFile = "names.txt";
        private const string _secondNameFile = "surnames.txt";
        private const string _userAgentFile = "UserAgents.txt";

        private static string GetRandomNameFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.AppendAllText(fileName, "\r\n");
            }
            var lines = File.ReadAllLines(fileName);
            var selectedLine = (new Random((int)(DateTime.Now.Ticks % int.MaxValue))).Next(0, lines.Length);
            return lines[selectedLine];
        }

        public static string GetRandomName()
        {
            return GetRandomNameFromFile(_firstNameFile);
        }

        public static string GetRandomSecondName()
        {
            return GetRandomNameFromFile(_secondNameFile);
        }

        public static (bool, string) GetRandomUserAgent()
        {
            do
            {
                var randomString = GetRandomNameFromFile(_userAgentFile);
                if (randomString.Contains(";"))
                {
                    var firstPart = randomString.Trim().Substring(0, randomString.IndexOf(';'));
                    var userAgent = randomString.Trim().Substring(randomString.IndexOf(';') + 1);
                    if (bool.TryParse(firstPart, out var result))
                    {
                        return (result, userAgent);
                    }
                    else
                        throw new FormatException("Неверный формат UserAgent в файле UserAgents.txt. Ожидается: <bool>;<UserAgent>");
                }
            } while (true);
        }
    }
}
