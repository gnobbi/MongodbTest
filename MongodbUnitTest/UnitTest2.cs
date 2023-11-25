using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongodbTestLib;
using MongodbTestLib.Entities;
using System.Diagnostics;
using B = MongoDB.Bson.BsonDocument;

namespace MongodbUnitTest
{
    public class Tests2
    {
        private IMongoCollection<Project> _projects;
        private IMongoCollection<SbomResult> _sbomResults;
        private IMongoCollection<BaseVulnerability> _baseVulnerabilities;
        private IMongoCollection<Component> _components;
        private IMongoCollection<Composition> _compositions;
        private string _searchTerm;

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
            _searchTerm = "a";
            //DbSeed.Seed(20);
        }

        [Test]
        public void GetAllProjects_manual()
        {
            ManualStrategy();
        }

        private SearchResponse ManualStrategy()
        {
            var result = new SearchResponse();
            var foundComponents = _components.AsQueryable().Where(x => x.Name.Contains(_searchTerm)).ToList();
            foreach (var foundComponent in foundComponents)
            {
                var r = new SearchResponseItem()
                {
                    Component = $"{foundComponent.Name}@{foundComponent.Version}"
                };
                var sbomResultIds = _compositions.AsQueryable().Where(x => x.ComponentId == foundComponent.Id).Select(x => x.SbomResultId).ToList();
                var sbomResults = _sbomResults.AsQueryable().Where(x => sbomResultIds.Contains(x.Id)).ToList();
                var projects = _projects.AsQueryable().Where(x => x.SbomResultIds.Any(x => sbomResultIds.Contains(x))).ToList();

                foreach (var project in projects)
                {
                    var r2 = new SearchProjectResponse()
                    {
                        Description = project.Description,
                        IId = project.Id.ToString(),
                        Name = project.Name
                    };

                    var r3 = new List<SbomResultResponse>();
                    foreach (var sbomResult in sbomResults.Where(x => project.SbomResultIds.Any(x1 => sbomResultIds.Contains(x1))))
                    {
                        r3.Add(new SbomResultResponse()
                        {
                            IId = sbomResult.Id.ToString(),
                            Version = sbomResult.Version
                        });
                    }
                    r2.SbomResults = r3;
                    r.Projects.Add(r2);
                }

                result.Results.Add(r);
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

            Assert.AreEqual(r1.Results.Count, r2.Results.Count);
            for (int i = 0; i < r1.Results.Count; i++)
            {
                Assert.AreEqual(r1.Results[i].Projects.Count, r1.Results[i].Projects.Count);
                for (int j = 0; j < r1.Results[i].Projects.Count; j++)
                {
                    Assert.AreEqual(r1.Results[i].Projects[j].SbomResults.Count, r1.Results[i].Projects[j].SbomResults.Count);

                }
            }

        }
        [Test]
        public void Peformance_manual()
        {

            for (var i = 0; i < 10; i++)
            {
                var r1 = ManualStrategy();

            }
        }

        [Test]
        public void Peformance_aggregation()
        {

            for (var i = 0; i < 10; i++)
            {
                var r2 = AggregationStrategy();

            }
        }

        public record dto(
            string ComponentName,
            string ProjectId,
            string ProjectName,
            string ProjectDescription,
            string SbomResultId,
            string SbomResultVersion
            );

        private SearchResponse AggregationStrategy()
        {
            var pipeline = new B[]
            {
                new B("$match",new B("Name", new B("$regex", _searchTerm))),
                new B("$lookup", new B {
                        { "from", "Compositions" },
                        { "localField", "_id" },
                        { "foreignField", "ComponentId" },
                        { "as", "Compositions" }
                    }),
                new B("$unwind", new B("path", "$Compositions")),
                new B("$lookup", new B {
                        { "from", "SbomResults" },
                        { "localField", "Compositions.SbomResultId" },
                        { "foreignField", "_id" },
                        { "as", "SbomResult" }
                    }),
                new B("$unwind", new B("path", "$SbomResult")),
                new B("$lookup", new B {
                        { "from", "Projects" },
                        { "localField", "SbomResult.ProjectId" },
                        { "foreignField", "_id" },
                        { "as", "Project" }
                    }),
                new B("$unwind", new B("path", "$Project")),
                new B("$project",
                new B
                    {
                        { "_id", 0 },
                        { "ComponentName", new B("$concat", new BsonArray { "$Name", "@", "$Version"}) },
                        { "ProjectId", new B("$toString", "$Project._id") },
                        { "ProjectName", "$Project.Name" },
                        { "ProjectDescription", "$Project.Description" },
                        { "SbomResultId",new B("$toString", "$SbomResult._id") },
                        { "SbomResultVersion", "$SbomResult.Version" }
                    })
                };
            var aggregation = _components.Aggregate<BsonDocument>(pipeline).ToList();
                var response = aggregation.Select(bson  => BsonSerializer.Deserialize<dto>(bson)).ToList();
            var result = new SearchResponse()
            {
            };

            var group = response.GroupBy(x => x.ProjectId).ToList();
            foreach(var item in response)
            {
                result.Results.Add(new SearchResponseItem() { Component = item.ComponentName,
                    Projects = group.Select(x => new SearchProjectResponse()
                {
                        Description = x.First().ProjectDescription,
                        Name = x.First().ProjectName,
                        IId = x.First().ProjectId,
                        SbomResults = x.DistinctBy(x0 => x0.SbomResultId).Select(x1 => new SbomResultResponse() { IId = x1.SbomResultId, Version = x1.SbomResultVersion}).ToList()
                }).ToList()
                });
            }

            //var response = BsonSerializer.Deserialize<SearchResponse>(aggregation);
            return result;
        }
    }
}