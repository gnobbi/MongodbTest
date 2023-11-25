using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MongodbTestLib.Entities
{
    public record ID
    {
        public ObjectId Id { get; set; }
    }
    public record Project : ID
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CvssEnvironmentalVector { get; set; }
        public List<ObjectId> SbomResultIds { get; set; }
    }
    public record SbomResult : ID
    {
        public ObjectId ProjectId { get; set; }
        public string Version { get; set; }
        public List<Vulnerability> Vulnerabilities { get; set; }
    }

    public record Vulnerability
    {
        public string Id { get; set; }
        public ObjectId BaseVulnerabilityId { get; set; }
        public ObjectId ComponentId { get; set; }
        public string AuditState { get; set; }
        public string AuditCommentary { get; set; }
    }

    public record BaseVulnerability : ID
    {
        public string VulId { get; set; }
        public string VulDescription { get; set; }
        public string VulReferences { get; set; }
        public string CvssBaseVector { get; set; }
    }

    public record Component : ID
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public record Composition : ID
    {
        public ObjectId ComponentId { get; set; }
        public ObjectId SbomResultId { get; set; }
    }
}
