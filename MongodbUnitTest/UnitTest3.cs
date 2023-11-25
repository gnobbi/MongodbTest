using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongodbUnitTest
{
    public class UnitTest3
    {
        public void Test()
        {
            var pipeline = new BsonDocument[]
{
    new BsonDocument("$match",
    new BsonDocument("_id",
    new ObjectId("65615730253bac9743034cf3"))),
    new BsonDocument("$unwind",
    new BsonDocument("path", "$Vulnerabilities")),
    new BsonDocument("$lookup",
    new BsonDocument
        {
            { "from", "BaseVulnerabilities" },
            { "localField", "Vulnerabilities.BaseVulnerabilityId" },
            { "foreignField", "_id" },
            { "as", "BaseVulnerability" }
        }),
    new BsonDocument("$unwind",
    new BsonDocument("path", "$BaseVulnerability")),
    new BsonDocument("$lookup",
    new BsonDocument
        {
            { "from", "Components" },
            { "localField", "Vulnerabilities.ComponentId" },
            { "foreignField", "_id" },
            { "as", "Component" }
        }),
    new BsonDocument("$unwind",
    new BsonDocument("path", "$Component")),
    new BsonDocument("$project",
    new BsonDocument
        {
            { "VulnId", "$BaseVulnerability.VulId" },
            { "VulDescription", "$BaseVulnerability.VulDescription" },
            { "VulReferences", "$BaseVulnerability.VulReferences" }
        })
}
        }
    }
}
