using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;

namespace Solutionizer.Infrastructure {
    public class UpdateManager {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly Version _currentVersion;

        public UpdateManager(Version currentVersion) {
            _currentVersion = currentVersion;
        }

        public Func<Task<XDocument>> ReadLocalXml;
        public Func<Task<XDocument>> ReadRemoteXml;

        private List<Release> _releases;

        public Task ReadLocalReleases() {
            return ReadLocalXml().ContinueWith(t => {
                try {
                    _releases = ReadReleases(t.Result).ToList();
                } catch (Exception ex) {
                    _log.ErrorException("Reading local release file failed", ex);
                    _releases = new List<Release>();
                }
            });
        }

        public Task ReadRemoteReleases() {
            return ReadRemoteXml().ContinueWith(t => {
                try {
                    var releases = ReadReleases(t.Result);
                    var newReleases = releases.Where(r => _releases.All(_ => _.Version != r.Version)).ToList();
                    _releases.AddRange(newReleases);
                } catch (Exception ex) {
                    _log.ErrorException("Reading remote release file failed", ex);
                }
            });
        }

        public IEnumerable<Release> GetReleases() {
            return _releases ?? Enumerable.Empty<Release>();
        }

        public bool IsUpdateAvailable {
            get { return _releases.Any(r => r.Version > _currentVersion); }
        }

        public static IEnumerable<Release> ReadReleases(XDocument doc) {
            try {
                var releases = from release in doc.Descendants("Release")
                               select new Release {
                                   Version = Version.Parse(release.Element("Version").Value),
                                   PublishedAt = DateTimeOffset.ParseExact(release.Element("PublishedAt").Value, "u", null),
                                   Notes = release.Element("Notes").Value
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