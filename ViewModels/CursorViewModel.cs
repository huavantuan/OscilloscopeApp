using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OscilloscopeApp.ViewModels
{
    public partial class MeasurementViewModel : ObservableObject
    {
        [ObservableProperty]
        private int channel;

        [ObservableProperty]
        private double cursorAX;

        [ObservableProperty]
        private double cursorBX;

        [ObservableProperty]
        private bool isCursorASet;
        
        [ObservableProperty]
        private bool isTimeMeasurement;

        public ObservableCollection<CursorMeasurement> Measurements { get; } = new();

        public void SetCursor(double x, Func<int, double, double> getValueAtX)
        {
            if (!IsCursorASet)
            {
                CursorAX = x;
                IsCursorASet = true;
            }
            else
            {
                CursorBX = x;
                IsCursorASet = false;
                UpdateMeasurements(getValueAtX);
            }
        }

        private void UpdateMeasurements(Func<int, double, double> getValueAtX)
        {
            Measurements.Clear();
            double deltaX = Math.Abs(CursorBX - CursorAX);

            for (int i = 0; i < 8; i++)
            {
                double yA = getValueAtX(i, CursorAX);
                double yB = getValueAtX(i, CursorBX);
                Measurements.Add(new CursorMeasurement
                {
                    Channel = i + 1,
                    ValueA = yA,
                    ValueB = yB,
                    DeltaY = yB - yA
                });
            }

            Measurements.Add(new CursorMeasurement
            {
                Channel = 0,
                ValueA = CursorAX,
                ValueB = CursorBX,
                DeltaY = deltaX,
                IsTimeMeasurement = true
            });
        }
    }
}
