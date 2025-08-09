using System.Globalization;
using System.Windows.Data;

namespace YandexRegistration.Converters
{
    public class NullToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return "Не зарегистрированные пользователи";
            if (value is DateTime dt)
                return dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
