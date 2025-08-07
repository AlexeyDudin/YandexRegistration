namespace YandexRegistrationModel
{
    public static class NameHelper
    {
        private const string _firstNameFile = "names.txt";
        private const string _secondNameFile = "surnames.txt";

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
    }
}
