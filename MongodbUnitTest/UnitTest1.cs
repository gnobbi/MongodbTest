using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongodbTestLib;
using MongodbTestLib.Entities;

namespace MongodbUnitTest
{
    public class Tests
    {
        private IMongoCollection<Project> _projects;
        private IMongoCollection<SbomResult> _sbomResults;
        private IMongoCollection<BaseVulnerability> _baseVulnerabilities;
        private IMongoCollection<Component> _components;
        private IMongoCollection<Composition> _compositions;

        [OneTimeSetUp]
        public void Setup()
        {
            var client = new MongoClient("mongodb://user:pass@localhost:27017");
            var database = client.GetDatabase("Test");
            _projects = database.GetCollection<Project>("Projects");
            _sbomResults = database.GetCollection<SbomResult>("SbomResults");
            _baseVulnerabilities = database.GetCollection<BaseVulnerability>("BaseVulnerabilities");
            _components = database.GetCollection<Component>("Components");
            _compositions = database.GetCollection<Composition>("Compositions");
        }

        [Test]
        public void GetAllProjects_manual()
        {
            ManualStrategy();
        }

        private List<GetProjectResponse> ManualStrategy()
        {
            var result = new List<GetProjectResponse>();
            var projects = _projects.AsQueryable().ToList();
            foreach (var project in projects)
            {
                var r = new GetProjectResponse()
                {
                    CvssEnvironmentalVector = project.CvssEnvironmentalVector,
                    Description = project.Description,
                    Name = project.Name,
                    IId = project.Id.ToString(),
                    SbomResults = new()
                };

                foreach (var sbomResultId in project.SbomResultIds)
                {
                    var sbDb = _sbomResults.AsQueryable().FirstOrDefault(x => x.Id == sbomResultId);
                    var sb = new SbomResultResponse()
                    {
                        IId = sbomResultId.ToString(),
                        Version = sbDb.Version
                    };
                    r.SbomResults.Add(sb);
                }
                result.Add(r);
            }

            return result;
        }

        [Test]
        public void GetAllProjects_aggregate()
        {
            AggregationStrategy();
        }

        [Test]
        public void Comparison()
        {
            var r1 = ManualStrategy();
            var r2 = AggregationStrategy();

            for(var i = 0; i < r1.Count; i++)
            {
                r1[i].SbomResults.Sort((s0,s1) => s1.IId.CompareTo(s0.IId));
                r2[i].SbomResults.Sort((s0,s1) => s1.IId.CompareTo(s0.IId));
                Assert.AreEqual(r1[i].SbomResults, r2[i].SbomResults);

            }
        }
        [Test]
        public void Peformance_manual()
        {

            for (var i = 0; i < 100; i++)
            {
                var r1 = ManualStrategy();

            }
        }

        [Test]
        public void Peformance_aggregation()
        {

            for (var i = 0; i < 100; i++)
            {
                var r2 = AggregationStrategy();

            }
        }

        private List<GetProjectResponse> AggregationStrategy()
        {
            var aggregation = _projects.Aggregate()
                //.Project(new BsonDocument()
                //{
                //    //{ "Id", new BsonDocument{ { "$toString", "$_id" } } },
                //    //{ "CvssEnvironmentalVector", 1 },
                //    //{ "Description", 1 },
                //    //{ "Name", 1 },
                //    { "SbomResults", new BsonDocument
                //    {
                //        { "$map", new BsonDocument 
                //        {
                //            { "input", "$SbomResults" },
                //            { "as", "item"},
                //            {
                //                "in", new BsonDocument
                //                {
                //                    { "$convert", new BsonDocument
                //                    {
                //                        { "input", "$$item" },
                //                        { "to", "objectId"}
                //                    }
                //                    }
                //                }
                //            }
                //            } 
                //        }
                //        }
                //    }
                //})
                .Lookup("SbomResults", "SbomResultIds", "_id", "SbomResults")
                .Project(new BsonDocument
                {
                    { "IId", new BsonDocument{ { "$toString", "$_id" } } },
                    { "_id", 0 },
                    { "CvssEnvironmentalVector", 1 },
                    { "Description", 1 },
                    { "Name", 1 },
                    { "SbomResults", new BsonDocument{
                        {"$map", new BsonDocument
                        {
                            { "input", "$SbomResults" },
                            { "as", "sb" },
                            {
                                "in", new BsonDocument
                                {
                                    { "IId", new BsonDocument{ { "$toString", "$$sb._id" } } },
                                    { "Version", "$$sb.Version" }
                                }
                            }
                        }
                        }
                    }},

                })
                .ToList();

            var response = aggregation.Select(bson => BsonSerializer.Deserialize<GetProjectResponse>(bson)).ToList();
            return response;
        }
    }
}