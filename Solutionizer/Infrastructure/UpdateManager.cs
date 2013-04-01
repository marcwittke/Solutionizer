﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;

namespace Solutionizer.Infrastructure {
    public class UpdateManager {
        private static readonly Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private Version _currentVersion;
        private readonly string _releaseFileName;

        public UpdateManager(Version currentVersion, string releaseFileName) {
            _currentVersion = currentVersion;
            _releaseFileName = releaseFileName;
        }

        public Func<Task<string>> ReadXml;

        private List<Release> _releases;

        public void ReadLocalReleases() {
            if (!File.Exists(_releaseFileName)) {
                _releases = new List<Release>();
                return;
            }

            try {
                var doc = XDocument.Load(_releaseFileName);
                _releases = ReadReleases(doc).ToList();
            } catch (Exception ex) {
                _log.ErrorException("Reading local release file failed", ex);
                _releases = new List<Release>();
            }
        }

        public Task CheckVersion() {
            return ReadXml().ContinueWith(content => {
                var doc = XDocument.Parse(content.Result);
                var releases = ReadReleases(doc);
                _releases.AddRange(releases.Where(r => _releases.All(_ => _.Version != r.Version)));
            });
        }

        public IEnumerable<Release> GetReleases() {
            return _releases ?? Enumerable.Empty<Release>();
        }

        public static IEnumerable<Release> ReadReleases(XDocument doc) {
            var releases = from release in doc.Descendants("Release")
                           select new Release {
                               Version = Version.Parse(release.Element("Version").Value),
                               PublishedAt = DateTimeOffset.ParseExact(release.Element("PublishedAt").Value, "u", null),
                               Notes = release.Element("Notes").Value
                           };
            return releases;
        }

        public static XDocument WriteReleases(IEnumerable<Release> releases) {
            return new XDocument(
                new XElement("Releases",
                             from r in releases
                             select new XElement("Release",
                                                 new XElement("Version", r.Version.ToString()),
                                                 new XElement("PublishedAt", r.PublishedAt.ToString("u")),
                                                 new XElement("Notes", r.Notes))

                    ));
        }
    }

    public class Release {
        public Version Version { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public string Notes { get; set; }
    }
}