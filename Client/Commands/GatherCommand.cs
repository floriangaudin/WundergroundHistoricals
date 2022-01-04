using Client.Options;
using CommandLine;
using HtmlAgilityPack;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client.Commands
{
    [Verb("gather", HelpText = "Retrieve data from wunderground")]
    internal sealed class GatherCommand
    {
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

        private static GatherOptions options;


        //Wundergound Table
        //Index     : 0     1           2           3           4       5       6       7           8               9               10  11
        //Headers   : Time  Temperature Dew Point   Humidity	Wind	Speed	Gust	Pressure	Precip. Rate.	Precip. Accum.	UV	Solar
        private enum Headers : byte
        {
            time,
            temperature,
            dewPoint,
            humidity,
            wind,
            speed,
            gust,
            pressure,
            precipitationRate,
            precipitationAccumulation,
            uv,
            solar
        }

        public static async Task Execute(GatherOptions Options)
        {
            options = Options;

            DateTime dateTime;
            short diffDays = 0;

            if (string.IsNullOrEmpty(options.Date))
            {
                dateTime = DateTime.Now.AddDays(-1);
            }
            else
            {
                //create date from custom date
                dateTime = DateTime.ParseExact(options.Date, "dd/MM/yyyy", CultureInfo.CurrentCulture);

                //compare days between today's date and custom date
                TimeSpan ts = DateTime.Now - dateTime;
                diffDays = Convert.ToInt16(Math.Floor(ts.TotalDays));

                //remove days difference for search filter
                dateTime = DateTime.Now.AddDays(-diffDays);

            }

            HtmlDocument htmlDocument = await GetAsync(new Uri("https://www.wunderground.com/dashboard/pws/"), options.PwsIdentifier, dateTime);


            if (htmlDocument is null)
                return;

            HtmlDocument htmlTable = GetHtmlTable(htmlDocument, "//lib-history-table/div/div/div/table");

            List<Record> records = new();

            foreach (var node in htmlTable.DocumentNode.SelectNodes("//tbody/tr"))
            {
                HtmlDocument htmlData = new();
                htmlData.LoadHtml(node.InnerHtml);

                var row = GetRowValues(htmlData.DocumentNode.SelectNodes("/td"));

                Record record = new()
                {
                    temperature = Convert.ToSingle(row["temperature"], CultureInfo.InvariantCulture.NumberFormat),
                    dewPoint = Convert.ToSingle(row["dewPoint"], CultureInfo.InvariantCulture.NumberFormat),
                    humidity = Convert.ToSByte(row["humidity"], CultureInfo.InvariantCulture.NumberFormat),
                    wind = (WindDirections)Enum.Parse(typeof(WindDirections), row["wind"]),
                    speed = Convert.ToSingle(row["speed"], CultureInfo.InvariantCulture.NumberFormat),
                    gust = Convert.ToSingle(row["gust"], CultureInfo.InvariantCulture.NumberFormat),
                    pressure = Convert.ToSingle(row["pressure"], CultureInfo.InvariantCulture.NumberFormat),
                    precipitationRate = Convert.ToSingle(row["precipitationRate"], CultureInfo.InvariantCulture.NumberFormat),
                    precipitationAccumulation = Convert.ToSingle(row["precipitationAccumulation"], CultureInfo.InvariantCulture.NumberFormat),
                    uv = Convert.ToSByte(row["uv"]),
                    solar = Convert.ToSingle(row["solar"], CultureInfo.InvariantCulture.NumberFormat),
                };


                if (diffDays == 0)
                    record.time = DateTime.Parse(row["time"]).AddDays(-1);
                else
                    record.time = DateTime.Parse(row["time"]).AddDays(-diffDays);

                //converts imperials units to metrics units if needed
                if (options.Units.Equals(GatherOptions.units.metrics))
                    record.ConvertToMetric();

                records.Add(record);

            }
            Console.Write($" > records = {records.Count}\n");

            Save(records, Path.Combine(options.Path, options.PwsIdentifier.ToUpper()), $"{options.PwsIdentifier.ToUpper()}-{dateTime:ddMMyyyy}");


        }



        static Dictionary<string,string> GetRowValues(HtmlNodeCollection tr)
        {
            //pattern used to split values without units
            //37.5 °F or 37.5&nbsp;°F becomes 37.5, 25 mph or 25&nbsp;mph becomes 25, etc.
            Regex regex = new(@"\s|&nbsp;");

            Dictionary<string,string> row = new();

            for (byte i = 0; i < tr.Count; i++)
            {

                var val = regex.Split(tr[i].InnerText).First();

                if(i == (byte)Headers.pressure && val != "--")
                {
                    row.Add(((Headers)i).ToString(), val);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(val) && val != "--")
                {
                    row.Add(((Headers)i).ToString(), val);
                    continue;
                }
                else
                {
                    row.Add(((Headers)i).ToString(), "-1");
                    continue;
                }

            }

            return row;

        }

        static void Save(IList<Record> records, string path, string filename)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            try
            {
                if (options.Format.Equals(GatherOptions.formats.json))
                {
                    File.WriteAllText($"{Path.Combine(path, filename)}.{options.Format}", JsonConvert.SerializeObject(records, Formatting.Indented));
                }

                if (options.Format.Equals(GatherOptions.formats.csv))
                {
                    using StreamWriter writer = new($"{Path.Combine(path, filename)}.{options.Format}");
                    using CsvHelper.CsvWriter csvWriter = new(writer, new (CultureInfo.InvariantCulture) { Delimiter = ";", Encoding = System.Text.Encoding.UTF8 });
                    csvWriter.WriteRecords(records);
                }

                Console.WriteLine($"File successfully saved ! ({Path.Combine(path, filename)}.{options.Format})");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {path} doesn't exists");
            }
        }

        /// <summary>
        /// Getting PWS html page from Wunderground.com
        /// </summary>
        /// <param name="baseAddress">http base address</param>
        /// <param name="pwsidentifier">station identifier</param>
        /// <param name="dateTime">date filter</param>
        /// <returns><seealso cref="HtmlDocument"/></returns>
        static async Task<HtmlDocument> GetAsync(Uri baseAddress, string pwsidentifier, DateTime dateTime)
        {
            //settings up a new httpclient
            using var httpClient = new HttpClient()
            { 
                BaseAddress = baseAddress,
                Timeout= new TimeSpan(0,0,30)
            };

            //setting a fake user-agent, no api-key needed
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            var endpoint = $"{pwsidentifier.ToUpper()}/table/{dateTime.Year}-{dateTime.Month}-{dateTime.Day}/{dateTime.Year}-{dateTime.Month}-{dateTime.Day}/daily";

            try
            {

                Console.Write($"GET: {baseAddress}{endpoint}");

                var response = await httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());

                    return htmlDocument;
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {baseAddress}{endpoint} an error occured during GET command");
                return default;
            }


        }

        static HtmlDocument GetHtmlTable(HtmlDocument htmlDocument, string XPath)
        {
            HtmlDocument htmlTable = new();
            htmlTable.LoadHtml(htmlDocument.DocumentNode.SelectSingleNode(XPath).InnerHtml);

            return htmlTable;
        }


    }
}
