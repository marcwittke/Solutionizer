using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using Solutionizer.Infrastructure;

namespace Solutionizer.Tests {
    [TestFixture]
    public class UpdateManagerTests {
        private const string RELEASE_XML = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Releases>
  <Release>
    <Version>1.0.0.0</Version>
    <PublishedAt>2013-04-01 13:00:00Z</PublishedAt>
    <Notes>tata</Notes>
  </Release>
</Releases>";

        [Test]
        public void CanReadXml() {
            var doc = XDocument.Parse(RELEASE_XML);
            var releases = UpdateManager.ReadReleases(doc).ToArray();

            Assert.AreEqual(1, releases.Length);
            Assert.AreEqual(new Version(1, 0, 0, 0), releases[0].Version);
            Assert.AreEqual("tata", releases[0].Notes);
        }

        [Test]
        public void CanWriteXml() {
            var releases = new[] {
                new Release {
                    Version = new Version(1, 0, 0, 0),
                    PublishedAt = new DateTimeOffset(2013, 4, 1, 12, 0, 0, TimeSpan.FromHours(-1)),
                    Notes = "tata"
                }
            };
            var doc = UpdateManager.WriteReleases(releases);
            Assert.AreEqual(RELEASE_XML, XDocumentToString(doc));
        }

        [Test]
        public void CanReadAndWrite() {
            var doc = XDocument.Parse(RELEASE_XML);
            var releases = UpdateManager.ReadReleases(doc);
            doc = UpdateManager.WriteReleases(releases);
            Assert.AreEqual(RELEASE_XML, XDocumentToString(doc));
        }

        private static string XDocumentToString(XDocument doc) {
            using (var mem = new MemoryStream())
            using (var writer = new XmlTextWriter(mem, System.Text.Encoding.UTF8)) {
                writer.Formatting = Formatting.Indented;
                doc.WriteTo(writer);
                writer.Flush();
                mem.Flush();
                mem.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(mem)) {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}