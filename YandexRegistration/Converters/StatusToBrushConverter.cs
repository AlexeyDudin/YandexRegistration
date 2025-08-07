using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using YandexRegistrationModel.Enums;

namespace YandexRegistration.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is YandexTaskStatus status)
            {
                return status switch
                {
                    YandexTaskStatus.Error => Brushes.Red,
                    YandexTaskStatus.Started => Brushes.LightBlue,
                    YandexTaskStatus.Ok => Brushes.LightGreen,
                    _ => Brushes.Transparent
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
