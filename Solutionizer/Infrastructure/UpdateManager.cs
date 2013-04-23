using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;

namespace Solutionizer.Infrastructure {
    public class ReleaseFileHandler {
        public Func<XDocument> ReadLocalXml;
        public Func<XDocument> ReadRemoteXml;
        public Action<XDocument> WriteLocalXml;
    }

    public class UpdateManager {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly Version _currentVersion;
        private readonly ReleaseFileHandler _releaseFileHandler;

        public UpdateManager(Version currentVersion, ReleaseFileHandler releaseFileHandler) {
            _currentVersion = currentVersion;
            _releaseFileHandler = releaseFileHandler;
        }

        private List<Release> _releases;

        public Task Init() {
            return Task.Factory.StartNew(InitImpl);
        }

        private void InitImpl() {
            ReadLocalReleases();
            ReadRemoteReleases();
            WriteLocalReleases();
        }

        public void ReadLocalReleases() {
            var xdoc = _releaseFileHandler.ReadLocalXml();
            _releases = ReadReleases(xdoc).ToList();
        }

        public void ReadRemoteReleases() {
            var doc = _releaseFileHandler.ReadRemoteXml();
            var releases = ReadReleases(doc);
            var newReleases = releases.Where(r => _releases.All(_ => _.Version != r.Version)).ToList();
            _releases.AddRange(newReleases);
        }

        public void WriteLocalReleases() {
            var doc = WriteReleases(_releases);
            _releaseFileHandler.WriteLocalXml(doc);
        }

        public IEnumerable<Release> GetReleases() {
            return _releases ?? Enumerable.Empty<Release>();
        }

        public bool IsUpdateAvailable {
            get { return _releases.Any(r => r.Version > _currentVersion); }
        }

        public static IEnumerable<Release> ReadReleases(XDocument doc) {
            if (doc == null) {
                _log.Info("No releases");
                return Enumerable.Empty<Release>();
            }
            try {
                var releases = from release in doc.Descendants("Release")
                               select new Release {
                                   Version = Version.Parse(release.Element("Version").Value),
                                   PublishedAt = DateTimeOffset.ParseExact(release.Element("PublishedAt").Value, "u", null),
                                   Notes = release.Element("Notes").Value,
                                   PackageUrl = release.Element("PackageUrl").Value,
                               };
                return releases;
            } catch (Exception ex) {
                _log.ErrorException("Reading releases failed", ex);
                return Enumerable.Empty<Release>();
            }
        }

        public static XDocument WriteReleases(IEnumerable<Release> releases) {
            return new XDocument(
                new XElement("Releases",
                             from r in releases
                             select new XElement("Release",
                                                 new XElement("Version", r.Version.ToString()),
                                                 new XElement("PublishedAt", r.PublishedAt.ToString("u")),
                                                 new XElement("Notes", r.Notes),
                                                 new XElement("PackageUrl", r.PackageUrl))

                    ));
        }
    }

    public class Release {
        public Version Version { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public string Notes { get; set; }
        public string PackageUrl { get; set; }
    }
}