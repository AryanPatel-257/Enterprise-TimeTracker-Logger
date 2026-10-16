using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TimeTracker
{
    public static class XMLHelper
    {
        private static readonly string xmlFile = "session_log.xml";

        public static void EnsureXmlFileExists()
        {
            if (!File.Exists(xmlFile))
            {
                new XDocument(new XElement("Sessions")).Save(xmlFile);
            }
        }

        public static void LogEvent(string type, DateTime time, bool emailSentHost1, bool emailSentHost2)
        {
            XDocument? doc = XDocument.Load(xmlFile);

            XElement? root = doc.Root;
            if (root != null)
            {
                XElement? today = root.Elements("Day").FirstOrDefault(d => (string)d.Attribute("date") == DateTime.Today.ToString("yyyy-MM-dd"));
                if (today == null)
                {
                    today = new XElement("Day", new XAttribute("date", DateTime.Today.ToString("yyyy-MM-dd")));
                    doc.Root?.Add(today);
                }

                int count = today.Elements(type).Count() + 1;

                today.Add(new XElement(type, new XAttribute("id", count), new XAttribute("time", time.ToString("HH:mm:ss")), new XAttribute("emailSentHost1", emailSentHost1), new XAttribute("emailSentHost2", emailSentHost2) ));

                var oldDataNodes = root.Elements("Day").Where(static d => { return DateTime.TryParse((string)d.Attribute("date"), out DateTime dateAttr) && dateAttr < DateTime.Today.AddDays(-7); }).ToList();

                foreach (var node in oldDataNodes)
                {
                    node.Remove();  // Removes each old Day element
                }

                doc.Save(xmlFile);
            }

           
        }

        public static string GetXmlFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "session_log.xml");
        }

        public static void CreateFreshXmlForToday()
        {
            string path = GetXmlFilePath();
            XDocument doc = new XDocument(new XElement("Sessions",
                new XElement("Day", new XAttribute("date", DateTime.Today.ToString("yyyy-MM-dd")))
            ));
            doc.Save(path);
        }
        public static bool IsOnBreak(out DateTime? breakStartTime)
        {
            breakStartTime = null;

            if (!File.Exists(xmlFile)) return false;

            XDocument doc = XDocument.Load(xmlFile);
            XElement today = doc.Root.Elements("Day").FirstOrDefault(d => (string)d.Attribute("date") == DateTime.Today.ToString("yyyy-MM-dd"));

            if (today != null)
            {
                var lastStart = today.Elements("BreakStart").LastOrDefault();
                if (lastStart != null)
                {
                    string? id = lastStart.Attribute("id")?.Value;
                    var matchingEnd = today.Elements("BreakEnd")
                                           .FirstOrDefault(be => be.Attribute("id")?.Value == id);

                    if (matchingEnd == null && DateTime.TryParse(lastStart.Attribute("time")?.Value, out DateTime bs))
                    {
                        breakStartTime = bs;
                        return true; // break still open
                    }
                }
            }

            return false;
        }

        public static void LoadSession(out DateTime? dayStart, out TimeSpan productive, out TimeSpan breaks)
        {
            dayStart = null;
            productive = TimeSpan.Zero;
            breaks = TimeSpan.Zero;

            if (!File.Exists(xmlFile)) return;

            XDocument doc = XDocument.Load(xmlFile);
            XElement today = doc.Root.Elements("Day")
                .FirstOrDefault(d => (string)d.Attribute("date") == DateTime.Today.ToString("yyyy-MM-dd"));

            if (today != null)
            {
                // 1. DayStart
                var start = today.Elements("DayStart").OrderBy(e => (string)e.Attribute("time")).FirstOrDefault();
                if (start != null && DateTime.TryParse(start.Attribute("time")?.Value, out DateTime ds))
                {
                    dayStart = DateTime.Today.Add(ds.TimeOfDay);
                }

                // 2. BreakStart and BreakEnd
                var breakStarts = today.Elements("BreakStart").ToList();
                var breakEnds = today.Elements("BreakEnd").ToList();

                foreach (var bs in breakStarts)
                {
                    string id = bs.Attribute("id")?.Value;
                    if (!DateTime.TryParse(bs.Attribute("time")?.Value, out DateTime startTime)) continue;

                    var matchingEnd = breakEnds.FirstOrDefault(be => be.Attribute("id")?.Value == id);

                    if (matchingEnd != null && DateTime.TryParse(matchingEnd.Attribute("time")?.Value, out DateTime endTime))
                    {
                        if (endTime > startTime)
                            breaks += (endTime - startTime);
                    }
                    else
                    {
                        // This break was started but not ended – it's ongoing
                        breaks += DateTime.Now - startTime;
                    }
                }

                // 3. Productive time = now - start - breaks
                if (dayStart.HasValue)
                {
                    productive = DateTime.Now - dayStart.Value - breaks;
                    if (productive < TimeSpan.Zero)
                        productive = TimeSpan.Zero;
                }
            }
        }


    }
}
