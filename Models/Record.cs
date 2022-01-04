using Models.Interfaces;
using System;

namespace Models
{
    public class Record : IValueConverter
    {
        public DateTime time { get; set; }
        public float temperature { get; set; } = -1;
        public float dewPoint { get; set; } = -1;
        public sbyte humidity { get; set; } = 0;
        public WindDirections wind { get; set; } = WindDirections.Unknow;
        public float speed { get; set; } = -1;
        public float gust { get; set; } = -1;
        public float pressure { get; set; } = -1;
        public float precipitationRate { get; set; } = -1;
        public float precipitationAccumulation { get; set; } = -1;
        public sbyte uv { get; set; } = -1;
        public float solar { get; set; } = -1;

        public void ConvertToMetric()
        {
            if(!temperature.Equals(-1))
                temperature = (temperature - 32) * (5f / 9f);

            if (!dewPoint.Equals(-1))
                dewPoint = (dewPoint - 32) * (5f / 9f);

            if (!speed.Equals(-1))
                speed *= 1.609344f;

            if (!gust.Equals(-1))
                gust *= 1.609344f;

            if (!pressure.Equals(-1))
                pressure *= 33.8637526f;

            if (!precipitationRate.Equals(-1))
                precipitationRate *= 25.4f;

            if (!precipitationAccumulation.Equals(-1))
                precipitationAccumulation *= 25.4f;
        }

    }

}